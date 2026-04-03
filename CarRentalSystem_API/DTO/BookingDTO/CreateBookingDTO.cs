namespace CarRentalSystem_API.DTO.BookingDTO
{
    public class CreateBookingDTO
    {
        public int UserID { get; set; }
        public int VehicleID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? PromotionCode { get; set; }
    }
}
