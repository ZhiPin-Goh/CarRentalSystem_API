namespace CarRentalSystem_API.DTO.BookingDTO
{
    public class CreateBookingDTO
    {
        public string  UserID { get; set; }
        public int VehicleID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalPaidAmount { get; set; }
        public int? PromotionID { get; set; }
    }
}
