namespace CarRentalSystem_API.Models
{
    public class Vehicle
    {
        public int VehicleID { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public string LicensePlate { get; set; }
        public decimal DailyRate { get; set; }
        public string Status { get; set; }//
        public string Type { get; set; }
        public string FuelType { get; set; }//
        public string? SpecsInfo { get; set; }
        public DateTime? RoadTaxExpiryDate { get; set; }
        public DateTime? InsuranceExpiryDate { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<VehicleImage> VehicleImages { get; set; }

    }
    
}
