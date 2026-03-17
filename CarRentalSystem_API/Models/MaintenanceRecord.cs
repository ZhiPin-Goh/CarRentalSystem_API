using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem_API.Models
{
    public class MaintenanceRecord
    {
        public int ID { get; set; }
        public int VehicleID { get; set; }
        [ForeignKey("VehicleID")]
        public virtual Vehicle Vehicle { get; set; }
        public string Description { get; set; }
        public decimal Cost { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Handler { get; set; }

    }
}
