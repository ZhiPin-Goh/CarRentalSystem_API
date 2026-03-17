using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem_API.Models
{
    public class Transaction
    {
        public int TransactionID { get; set; }

        public int BookingID { get; set; }
        [ForeignKey("BookingID")]
        public virtual Booking Booking { get; set; }

        public decimal Amount { get; set; }

        public DateTime TransactionDate { get; set; }

        public string PaymentMethod { get; set; } = "Touch 'n Go eWallet";

        public string Type { get; set; }

        public string Status { get; set; }//
        public string TransactionCode { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpper();
    }

   
}
