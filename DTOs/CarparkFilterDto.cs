namespace carpark_info_assignment.DTOs;

public class CarparkFilterDto
{
    public bool? FreeParking { get; set; }
    public bool? NightParking { get; set; }
    public decimal? VehicleHeight { get; set; }
}