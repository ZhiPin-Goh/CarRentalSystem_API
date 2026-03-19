namespace CarRentalSystem_API.DTO.UserDTO
{
    public class ChangePassword
    {
        public int UserID { get; set; }
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
    }
}