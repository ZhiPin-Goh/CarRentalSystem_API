using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManageBookingController : Controller
    {
        private readonly AppDbContext _db;
        public ManageBookingController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("unavailable-dates/{vehicleid}")]
        public async Task<IActionResult> GetUnavailabelDates(int vehicleid)
        {
            var existingVechile = await _db.Vehicles.AnyAsync(x => x.VehicleID == vehicleid);
            if (!existingVechile)
                return NotFound(new
                {
                    error = "Vehicle not found",
                    Message = $"No vehicle found with ID {vehicleid}"
                });
            var booking = await _db.Bookings
                .Where(x => x.VehicleID == vehicleid &&
                x.EndDate >= DateTime.Today &&
                (x.Status == "Pending" || x.Status == "Confirmed"))
                .ToListAsync();
            var maintenances = await _db.MaintenanceRecords
                .Where(x => x.VehicleID == vehicleid &&
                x.EndDate >= DateTime.Today).ToListAsync();
            var unavailableDates = new List<object>();
            foreach (var b in booking)
            {
                unavailableDates.Add(new
                {
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Type = "Booking"
                });
            }
            foreach (var m in maintenances)
            {
                unavailableDates.Add(new
                {
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    Type = "Maintenance"
                });
            }
            return Ok(unavailableDates);
        }
    }
}
