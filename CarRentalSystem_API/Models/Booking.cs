using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem_API.Models
{
    public class Booking
    {
        [Key]
        public int BookingID { get; set; }
        public int UserID{ get; set; }
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        public int VehicleID { get; set; }
        [ForeignKey("VehicleID")]
        public virtual Vehicle Vehicle { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public decimal TotalPrice { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal FinalPaidAmount { get; set; }
        public int? PromotionID { get; set; }
        [ForeignKey("PromotionID")]
        public virtual Promotion Promotion { get; set; }

        public string Status { get; set; }//
        public bool IsExtended { get; set; }
        public DateTime? CreatedAt { get; set; }
        public virtual ICollection<Transaction> Transactions { get; set; }
    }
   
}
