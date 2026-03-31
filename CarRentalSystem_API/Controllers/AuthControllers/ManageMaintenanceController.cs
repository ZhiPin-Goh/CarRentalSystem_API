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
            if (createMaintenance.StartDate > createMaintenance.EndDate)
                return BadRequest("Start date cannot be later than end date.");
            if (createMaintenance.Cost < 0)
                return BadRequest("Cost cannot be negative.");

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
        [HttpPost]
        public async Task<IActionResult> UpdateMaintenance([FromBody] UpdateMaintenanceDTO updateMaintenance)
        {
            var existingRecord = await _db.MaintenanceRecords.FirstOrDefaultAsync(x => x.ID == updateMaintenance.ID);
            if (existingRecord == null)
                return NotFound($"Maintenance record with ID {updateMaintenance.ID} not found.");
            if (updateMaintenance.StartDate > updateMaintenance.EndDate)
                return BadRequest("Start date cannot be later than end date.");
            if (updateMaintenance.Cost < 0)
                return BadRequest("Cost cannot be negative.");

            bool isOverlapping = await _db.MaintenanceRecords.AnyAsync(x => x.VehicleID == existingRecord.VehicleID && x.ID != updateMaintenance.ID &&
                ((updateMaintenance.StartDate >= x.StartDate && updateMaintenance.StartDate <= x.EndDate) ||
                 (updateMaintenance.EndDate >= x.StartDate && updateMaintenance.EndDate <= x.EndDate) ||
                 (updateMaintenance.StartDate <= x.StartDate && updateMaintenance.EndDate >= x.EndDate)));
            if (isOverlapping)
                return BadRequest("The maintenance period overlaps with an existing maintenance record for the same vehicle.");

            _db.Entry(existingRecord).CurrentValues.SetValues(updateMaintenance);
            await _db.SaveChangesAsync();
            return Ok("Maintenance record updated successfully.");
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaintenance(int id)
        {
            var existingRecord = await _db.MaintenanceRecords.FirstOrDefaultAsync(x => x.ID == id);
            if (existingRecord == null)
                return NotFound($"Maintenance record with ID {id} not found.");

            // Change vehicle status to "Available" if the deleted maintenance record is currently active
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleID == existingRecord.VehicleID);
            if (vehicle != null && vehicle.Status == "Maintenance" &&
                existingRecord.StartDate <= DateTime.UtcNow && existingRecord.EndDate >= DateTime.UtcNow)
            {
                vehicle.Status = "Available";
            }

            _db.MaintenanceRecords.Remove(existingRecord);
            await _db.SaveChangesAsync();
            return Ok("Maintenance record deleted successfully.");
        }
    }
}
