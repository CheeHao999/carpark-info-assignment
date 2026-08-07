using System.ComponentModel.DataAnnotations.Schema;

namespace carpark_info_assignment.Models;

public class UserFavorite
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string CarParkNo { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CarParkNo))]
    public Carpark? Carpark { get; set; }
}