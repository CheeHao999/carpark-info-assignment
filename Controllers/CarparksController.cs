using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using carpark_info_assignment.Data;
using carpark_info_assignment.DTOs;
using carpark_info_assignment.Models;
using carpark_info_assignment.Services;

namespace carpark_info_assignment.Controllers;

/// <summary>
/// Manages carpark data ingestion, dynamic filtering, and user favorites.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CarparksController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICarparkBatchService _batchService;

    public CarparksController(AppDbContext context, ICarparkBatchService batchService)
    {
        _context = context;
        _batchService = batchService;
    }

    /// <summary>
    /// Uploads and processes a daily delta CSV file within an atomic database transaction.
    /// </summary>
    /// <param name="file">HDB carpark CSV dataset file.</param>
    /// <returns>Status message confirming batch ingestion result.</returns>
    /// <response code="200">File processed and records ingested successfully.</response>
    /// <response code="500">Transaction failed and all database changes were rolled back.</response>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UploadBatch(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("A valid CSV file is required.");

        using var stream = file.OpenReadStream();
        await _batchService.ProcessBatchAsync(stream);
        return Ok(new { message = "Batch file processed successfully." });
    }

    /// <summary>
    /// Filters carparks dynamically by vehicle height, night parking, and free parking options.
    /// </summary>
    /// <param name="filter">Query parameters for dynamic filtering criteria.</param>
    /// <returns>A list of matching carpark records.</returns>
    /// <response code="200">Returns matching carparks.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Carpark>>> GetCarparks([FromQuery] CarparkFilterDto filter)
    {
        var query = _context.Carparks.AsQueryable();

        if (filter.FreeParking.HasValue && filter.FreeParking.Value)
            query = query.Where(c => !c.FreeParking.ToUpper().Equals("NO"));

        if (filter.NightParking.HasValue)
            query = query.Where(c => c.NightParking == filter.NightParking.Value);

        if (filter.VehicleHeight.HasValue)
            query = query.Where(c => c.GantryHeight >= filter.VehicleHeight.Value);

        return Ok(await query.ToListAsync());
    }

    /// <summary>
    /// Adds a designated carpark to a user's list of favorites.
    /// </summary>
    /// <param name="dto">User ID and Car Park Number payload.</param>
    /// <returns>The created user favorite entity.</returns>
    /// <response code="201">Carpark added to favorites successfully.</response>
    /// <response code="404">Carpark number was not found in database.</response>
    /// <response code="409">Carpark is already present in user's favorites.</response>
    [HttpPost("favorites")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteDto dto)
    {
        var carparkExists = await _context.Carparks.AnyAsync(c => c.CarParkNo == dto.CarParkNo);
        if (!carparkExists)
            return NotFound($"Carpark {dto.CarParkNo} not found.");

        var exists = await _context.UserFavorites
            .AnyAsync(f => f.UserId == dto.UserId && f.CarParkNo == dto.CarParkNo);

        if (exists)
            return Conflict("Carpark is already in user's favorites.");

        var favorite = new UserFavorite { UserId = dto.UserId, CarParkNo = dto.CarParkNo };
        _context.UserFavorites.Add(favorite);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(AddFavorite), new { id = favorite.Id }, favorite);
    }
}