namespace CarRentalSystem_API.DTO.BookingDTO
{
    public class ExtendPaymentDTO
    {
        public int BookingID { get; set; }
        public DateTime NewEndDate { get; set; }
        public string PaymentMethod { get; set; }
    }
}
