namespace CarRentalSystem_API.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Password { get; set; }
        public string Status { get; set; }//
        public string? DriverLicenseNumber { get; set; }
        public string? TelegramID { get; set; }
        public string? OTP { get; set; }
        public DateTime? OTPGeneratedAt { get; set; }
        public string? Role { get; set; }   
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<TokenActivity> TokenActivities { get; set; }
    }
  
}
