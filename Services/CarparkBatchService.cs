using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using carpark_info_assignment.Data;
using carpark_info_assignment.DTOs;
using carpark_info_assignment.Models;

namespace carpark_info_assignment.Services;

/// <summary>
/// Business logic service for handling carpark CSV parsing and atomic database transactions.
/// </summary>
public interface ICarparkBatchService
{
    /// <summary>
    /// Processes a CSV stream containing daily delta carpark data within an atomic transaction.
    /// </summary>
    /// <param name="fileStream">Stream containing HDB carpark CSV dataset contents.</param>
    Task ProcessBatchAsync(Stream fileStream);
}

/// <summary>
/// Business logic service for handling carpark CSV parsing and atomic database transactions.
/// </summary>
public class CarparkBatchService : ICarparkBatchService
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CarparkBatchService"/> class.
    /// </summary>
    /// <param name="context">Database context instance for data persistence operations.</param>
    public CarparkBatchService(AppDbContext context) => _context = context;

    /// <summary>
    /// Reads a CSV stream, maps records into carpark entities, and performs atomic upsert operations.
    /// Rolls back the entire database transaction if parsing or database execution encounters an error.
    /// </summary>
    /// <param name="fileStream">Stream containing HDB carpark CSV dataset contents.</param>
    /// <exception cref="InvalidOperationException">Thrown when ingestion fails to signal transaction rollback.</exception>
    public async Task ProcessBatchAsync(Stream fileStream)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            using var reader = new StreamReader(fileStream);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<CarparkCsvRecord>();

            foreach (var record in records)
            {
                var entity = new Carpark
                {
                    CarParkNo = record.CarParkNo,
                    Address = record.Address,
                    XCoord = record.XCoord,
                    YCoord = record.YCoord,
                    CarParkType = record.CarParkType,
                    TypeOfParkingSystem = record.TypeOfParkingSystem,
                    ShortTermParking = record.ShortTermParking,
                    FreeParking = record.FreeParking,
                    NightParking = record.NightParking.Trim().Equals("YES", StringComparison.OrdinalIgnoreCase),
                    GantryHeight = record.GantryHeight,
                    CarParkDecks = record.CarParkDecks,
                    CarParkBasement = record.CarParkBasement.Trim().Equals("Y", StringComparison.OrdinalIgnoreCase)
                };

                var existing = await _context.Carparks.FindAsync(entity.CarParkNo);
                if (existing == null) 
                    await _context.Carparks.AddAsync(entity);
                else 
                    _context.Entry(existing).CurrentValues.SetValues(entity);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException("Batch execution failed. Database transaction rolled back.", ex);
        }
    }
}