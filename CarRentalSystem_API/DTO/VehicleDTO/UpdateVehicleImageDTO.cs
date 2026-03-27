namespace CarRentalSystem_API.DTO.VehicleDTO
{
    public class UpdateVehicleImageDTO
    {
        public int VehicleID { get; set; }
        public IFormFile? PrimaryImage { get; set; }
        public List<IFormFile>? AdditionalImages { get; set; }
    }
}
