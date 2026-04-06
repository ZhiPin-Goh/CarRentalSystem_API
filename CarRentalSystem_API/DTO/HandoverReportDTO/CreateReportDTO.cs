namespace CarRentalSystem_API.DTO.HandoverReportDTO
{
    public class CreateReportDTO
    {
        public int BookingID { get; set; }
        public int Mileage { get; set; }
        public string FuelLevel { get; set; }
        public IFormFile? VehicleImage1 { get; set; }
        public IFormFile? VehicleImage2 { get; set; }
        public string? Remarks { get; set; }
    }
}
