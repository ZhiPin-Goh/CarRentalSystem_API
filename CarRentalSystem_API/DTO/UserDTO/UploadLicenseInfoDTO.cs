namespace CarRentalSystem_API.DTO.UserDTO
{
    public class UploadLicenseInfoDTO
    {
        public string DriverLicenseNumber { get; set; }
        public FormFile DriverLicenseImage { get; set; }
    }
}
