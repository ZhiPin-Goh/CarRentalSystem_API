using CarRentalSystem_API.Models;

namespace CarRentalSystem_API.DTO.VehicleDTO
{
    public class CreateVehicleDTO
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string LicensePlate { get; set; }
        public decimal DailyRate { get; set; }
        public string Type { get; set; }
        public string FuelType { get; set; }
        public IFormFile ImageVehicle { get; set; }
        public string? SpecsInfo { get; set; }
        
    }
   
}
