using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.StaffControllers
{
    [Route("api/staff/vehicle")]
    [ApiController]
    [Tags("Staff Vehicle Management")]
    [Authorize(Roles = "Staff")]
    public class VehicleManagementController : Controller
    {
        private readonly AppDbContext _db;
        public VehicleManagementController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("search/{licensePlate}")]
        public async Task<IActionResult> SearchVehicle(string licensePlate)
        {
            var vehicle = await _db.Vehicles
                .Include(v => v.VehicleImages)
                .Where(v => v.LicensePlate == licensePlate)
                .Select(v => new
                {
                    v.VehicleID,
                    v.Brand,
                    v.Model,
                    v.Year,
                    v.LicensePlate,
                    v.DailyRate,
                    v.Status,
                    v.Type,
                    v.FuelType,
                    v.SpecsInfo,
                    PrimaryImageURL = v.VehicleImages
                   .Where(img => img.IsPrimary)
                   .Select(img => img.ImageURL)
                   .FirstOrDefault()
                }).FirstOrDefaultAsync();
            if (vehicle == null)
                return NotFound(new
                {
                    error = "Vehicle Not Found",
                    message = $"No vehicle found with license plate {licensePlate}. Please check the license plate and try again."
                });
            return Ok(vehicle);
        }
    }
}
