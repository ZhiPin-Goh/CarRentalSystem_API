namespace CarRentalSystem_API.DTO.BookingDTO
{
    public class CreateBookingDTO
    {
        public int VehicleID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? PromotionCode { get; set; }
        public string? DeliveryAddress { get; set; }
        public string HandoverMethod { get; set; }
        public int? DeliveryAreaID { get; set; }
    }
}
