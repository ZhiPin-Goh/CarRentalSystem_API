using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRentalSystem_API.Models
{
    public class HandoverReport
    {
        [Key]
        public int ReportID { get; set; }
        public int BookingID { get; set; }
        [ForeignKey(nameof(BookingID))]
        public virtual Booking Booking { get; set; }
        public string ReportType { get; set; } // Check-out or Check-in
        public int StaffID { get; set; }
        [ForeignKey(nameof(StaffID))]
        public virtual User Staff { get; set; }
        public int Mileage { get; set; }
        public string FuelLevel { get; set; }
        public string? VehicleImage1 { get; set; }
        public string? VehicleImage2 { get; set; }
        public string? Remarks { get; set; }
        public DateTime HandoverTime { get; set; }
    }
}
