namespace CarRentalSystem_API.DTO.UserDTO
{
    public class UploadLicenseInfoDTO
    {
        public string DriverLicenseNumber { get; set; }
        public IFormFile DriverLicenseImage { get; set; }
    }
}
