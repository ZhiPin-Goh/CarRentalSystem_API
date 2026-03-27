namespace CarRentalSystem_API.DTO.VehicleDTO
{
    public class UpdateVehicleInformationDTO
    {
        public int VechicleID { get; set; }
        public string? Brand { get; set; }
        public string? Model { get; set; }
        public int? Year { get; set; }
        public string? LicensePlate { get; set; }
        public decimal? DailyRate { get; set; }
        public string? Type { get; set; }
        public string? FuelType { get; set; }
        public string? SpecsInfo { get; set; }
    }
}
