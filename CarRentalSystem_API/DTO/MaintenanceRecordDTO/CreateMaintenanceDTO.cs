namespace CarRentalSystem_API.DTO.MaintenanceRecordDTO
{
    public class CreateMaintenanceDTO
    {
        public int VehicleID { get; set; }
        public string Description { get; set; }
        public decimal Cost { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Handler { get; set; }
    }
}
