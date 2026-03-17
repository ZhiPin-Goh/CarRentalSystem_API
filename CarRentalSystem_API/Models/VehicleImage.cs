using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem_API.Models
{
    public class VehicleImage
    {
        public int ID { get; set; }
        public int VehicleID { get; set; }
        [ForeignKey ("VehicleID")]
        public virtual Vehicle Vehicle { get; set; }
        public string ImageURL { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
