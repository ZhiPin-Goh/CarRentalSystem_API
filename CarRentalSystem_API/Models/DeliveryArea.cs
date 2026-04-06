using System.ComponentModel.DataAnnotations;

namespace CarRentalSystem_API.Models
{
    public class DeliveryArea
    {
        [Key]
        public int AreaID { get; set; }
        public string AreaName { get; set; }
        public decimal Fee { get; set; }
        public bool IsActive { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
    }
}
