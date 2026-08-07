using CsvHelper.Configuration.Attributes;

namespace carpark_info_assignment.DTOs;

public class CarparkCsvRecord
{
    [Name("car_park_no")] public string CarParkNo { get; set; } = string.Empty;
    [Name("address")] public string Address { get; set; } = string.Empty;
    [Name("x_coord")] public decimal XCoord { get; set; }
    [Name("y_coord")] public decimal YCoord { get; set; }
    [Name("car_park_type")] public string CarParkType { get; set; } = string.Empty;
    [Name("type_of_parking_system")] public string TypeOfParkingSystem { get; set; } = string.Empty;
    [Name("short_term_parking")] public string ShortTermParking { get; set; } = string.Empty;
    [Name("free_parking")] public string FreeParking { get; set; } = string.Empty;
    [Name("night_parking")] public string NightParking { get; set; } = string.Empty;
    [Name("gantry_height")] public decimal GantryHeight { get; set; }
    [Name("car_park_decks")] public int CarParkDecks { get; set; }
    [Name("car_park_basement")] public string CarParkBasement { get; set; } = string.Empty;
}