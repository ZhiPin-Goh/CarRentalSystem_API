namespace CarRentalSystem_API.DTO.BookingDTO
{
    public class BookingPriceRequestDTO
    {
        public int VehicleID { get; set; }
        public int? DeliveryAreaID { get; set; }
        public string? PromoCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
