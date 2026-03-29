using CarRentalSystem_API.DTO.MaintenanceRecordDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManageMaintenanceController : Controller
    {
        private readonly AppDbContext _db;
        public ManageMaintenanceController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllMaintenanceRecord()
        {
            var maintenanceRecords = await _db.MaintenanceRecords.ToListAsync();
            return Ok(maintenanceRecords);
        }
        [HttpPost]
        public async Task<IActionResult> CreateMaintenance([FromBody] CreateMaintenanceDTO createMaintenance)
        {
            var existingVehicle = await _db.Vehicles.AnyAsync(v => v.VehicleID == createMaintenance.VehicleID);
            if (!existingVehicle)
                return NotFound($"Vehicle with ID {createMaintenance.VehicleID} not found.");
            bool isOverlapping = await _db.MaintenanceRecords.AnyAsync(m =>
                m.VehicleID == createMaintenance.VehicleID &&
                ((createMaintenance.StartDate >= m.StartDate && createMaintenance.StartDate <= m.EndDate) ||
                 (createMaintenance.EndDate >= m.StartDate && createMaintenance.EndDate <= m.EndDate) ||
                 (createMaintenance.StartDate <= m.StartDate && createMaintenance.EndDate >= m.EndDate)));
            if (isOverlapping)
                return BadRequest("The maintenance period overlaps with an existing maintenance record for the same vehicle.");
            var maintenanceRecord = new MaintenanceRecord
            {
                VehicleID = createMaintenance.VehicleID,
                Description = createMaintenance.Description,
                Cost = createMaintenance.Cost,
                StartDate = createMaintenance.StartDate,
                EndDate = createMaintenance.EndDate,
                Handler = !string.IsNullOrEmpty(createMaintenance.Handler) ? createMaintenance.Handler : "Admin"
            };
            _db.MaintenanceRecords.Add(maintenanceRecord);
            await _db.SaveChangesAsync();
            return Ok("Maintenance record created successfully.");
        }
    }
}
