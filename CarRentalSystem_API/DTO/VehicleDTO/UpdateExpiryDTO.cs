namespace CarRentalSystem_API.DTO.VehicleDTO
{
    public class UpdateExpiryDTO
    {
        public int VehicleID { get; set; }
        public DateTime? RoadTaxExpiryDate { get; set; }
        public DateTime? InsuranceExpiryDate { get; set; }
    }
}
