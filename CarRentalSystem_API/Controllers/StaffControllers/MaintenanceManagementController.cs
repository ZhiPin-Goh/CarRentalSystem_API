using CarRentalSystem_API.DTO.MaintenanceRecordDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRentalSystem_API.Controllers.StaffControllers
{
    [ApiController]
    [Route("api/staff/maintenance")]
    [Tags("Staff Maintenance Management")]
    [Authorize(Roles = "Staff")]
    public class MaintenanceManagementController : Controller
    {
        private readonly AppDbContext _db;
        public MaintenanceManagementController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("maintenancerecords")]
        public async Task<IActionResult> GetAllMaintenanceRecord()
        {
            var maintenanceRecords = await _db.MaintenanceRecords.ToListAsync();
            return Ok(maintenanceRecords);
        }
        [HttpPost("createmaintenance")]
        public async Task<IActionResult> CreateMaintenance([FromBody] CreateMaintenanceDTO createMaintenance)
        {
            int staffID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (staffID == 0)
                return Unauthorized(new
                {
                    error = "Unauthorized",
                    message = "You must be logged in to perform this action."
                });
            var staff = await _db.Users.FindAsync(staffID);
            if (staff == null)
                return NotFound(new
                {
                    error = "User Not Found",
                    message = $"User with ID {staffID} not found."
                });
            var existingVehicle = await _db.Vehicles.AnyAsync(v => v.VehicleID == createMaintenance.VehicleID);
            if (!existingVehicle)
            {
                return NotFound(new
                {
                    error = "Vehicle Not Found",
                    message = $"Vehicle with ID {createMaintenance.VehicleID} not found."
                });
            }
            bool isOverlapping = await _db.MaintenanceRecords.AnyAsync(m =>
            m.VehicleID == createMaintenance.VehicleID &&
            ((createMaintenance.StartDate >= m.StartDate && createMaintenance.StartDate <= m.EndDate) ||
             (createMaintenance.EndDate >= m.StartDate && createMaintenance.EndDate <= m.EndDate) ||
             (createMaintenance.StartDate <= m.StartDate && createMaintenance.EndDate >= m.EndDate)));
            if (isOverlapping)
            {
                return BadRequest(new
                {
                    error = "Overlapping Maintenance Period",
                    message = "The maintenance period overlaps with an existing maintenance record for the same vehicle."
                });
            }
            if (createMaintenance.Cost < 0)
            {
                return BadRequest(new
                {
                    error = "Invalid Cost",
                    message = "Cost cannot be negative."
                });
            }

            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleID == createMaintenance.VehicleID);
            if (vehicle != null && vehicle.Status != "Maintenance")
            {
                vehicle.Status = "Maintenance";
            }

            var maintenanceRecord = new MaintenanceRecord
            {
                VehicleID = createMaintenance.VehicleID,
                Description = createMaintenance.Description,
                Cost = createMaintenance.Cost,
                StartDate = createMaintenance.StartDate,
                EndDate = createMaintenance.EndDate,
                Handler = !string.IsNullOrEmpty(createMaintenance.Handler) ? createMaintenance.Handler : staff.UserName
            };
            _db.MaintenanceRecords.Add(maintenanceRecord);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Maintenance record created successfully.",
                Information = new
                {
                    maintenanceRecord.ID,
                    maintenanceRecord.VehicleID,
                    maintenanceRecord.Description,
                    maintenanceRecord.Cost,
                    maintenanceRecord.StartDate,
                    maintenanceRecord.EndDate,
                    maintenanceRecord.Handler
                }
            });
        }
        [HttpPost("updatemaintenance")]
        public async Task<IActionResult> UpdateMaintenance([FromBody] UpdateMaintenanceDTO updateMaintenance)
        {
            var existingRecord = await _db.MaintenanceRecords.FirstOrDefaultAsync(x => x.ID == updateMaintenance.ID);
            if (existingRecord == null)
            {
                return NotFound(new
                {
                    error = "Maintenance Record Not Found",
                    message = $"Maintenance record with ID {updateMaintenance.ID} not found."
                });
            }
            if (updateMaintenance.StartDate > updateMaintenance.EndDate)
            {
                return BadRequest(new
                {
                    error = "Invalid Date Range",
                    message = "Start date cannot be later than end date."
                });
            }
            if (updateMaintenance.Cost < 0)
            {
                return BadRequest(new
                {
                    error = "Invalid Cost",
                    message = "Cost cannot be negative."
                });
            }
            bool isOverlapping = await _db.MaintenanceRecords.AnyAsync(x => x.VehicleID == existingRecord.VehicleID && x.ID != updateMaintenance.ID &&
            ((updateMaintenance.StartDate >= x.StartDate && updateMaintenance.StartDate <= x.EndDate) ||
             (updateMaintenance.EndDate >= x.StartDate && updateMaintenance.EndDate <= x.EndDate) ||
             (updateMaintenance.StartDate <= x.StartDate && updateMaintenance.EndDate >= x.EndDate)));
            if (isOverlapping)
            {
                return BadRequest(new
                {
                    error = "Overlapping Maintenance Period",
                    message = "The maintenance period overlaps with an existing maintenance record for the same vehicle."
                });
            }
            _db.Entry(existingRecord).CurrentValues.SetValues(updateMaintenance);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Maintenance record updated successfully.",
                Information = new
                {
                    existingRecord.ID,
                    existingRecord.VehicleID,
                    existingRecord.Description,
                    existingRecord.Cost,
                    existingRecord.StartDate,
                    existingRecord.EndDate,
                    existingRecord.Handler
                }
            });
        }
        [HttpPost("donemaintenance/{id}")]
        public async Task<IActionResult> DoneMaintenance(int id)
        {
            var existingRecord = await _db.MaintenanceRecords
                .Include(x => x.Vehicle)
                .FirstOrDefaultAsync(x => x.ID == id);
            if (existingRecord == null)
            {
                return NotFound(new
                {
                    error = "Maintenance Record Not Found",
                    message = $"Maintenance record with ID {id} not found."
                });
            }
            existingRecord.Vehicle.Status = "Available";
            existingRecord.EndDate = DateTime.Now;
            existingRecord.Handler = "Admin";
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Maintenance marked as done successfully.",
                Information = new
                {
                    existingRecord.ID,
                    existingRecord.VehicleID,
                    existingRecord.Description,
                    existingRecord.Cost,
                    existingRecord.StartDate,
                    existingRecord.EndDate,
                    existingRecord.Handler
                }
            });
        }
    }
}
