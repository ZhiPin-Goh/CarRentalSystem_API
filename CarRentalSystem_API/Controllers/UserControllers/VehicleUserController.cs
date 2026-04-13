using CarRentalSystem_API.DTO.VehicleDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/user/vehicle")]
    [Tags("User Vehicle Management")]
    [Authorize(Roles = "User")]
    public class VehicleUserController : Controller
    {
        private readonly AppDbContext _db;
        public VehicleUserController(AppDbContext db)
        {
            _db = db;
        }
     
        [HttpGet ("vehicles")]
        public async Task<IActionResult> GetHomePageVehicles()
        {
            var vehicles = await _db.Vehicles
                .Where(v => v.Status == "Available" || v.Status == "Rented")
                .Include(v => v.VehicleImages)
                .Take(6)
                .Select(v => new
                {
                    v.VehicleID,
                    v.Brand,
                    v.Model,
                    v.Type,
                    v.DailyRate,
                    IsAvaliableNow = v.Status == "Available",
                    PrimaryImageURL = v.VehicleImages
                   .Where(img => img.IsPrimary)
                   .Select(img => img.ImageURL)
                   .FirstOrDefault()
                })
                .ToListAsync();
            var activePromtions = await _db.Promotions
                .Where(p => p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .Select(p => new
                {
                    p.PromotionID,
                    p.DiscountPercentage,
                })
                .ToListAsync();
            return Ok(new
            {
                Vehicles = vehicles,
                Promotions = activePromtions
            });
        }
        [HttpGet ("search")]
        public async Task<IActionResult> SearchTypeSearch([FromQuery] string? brand, [FromQuery] string? type, [FromQuery] decimal? maxPrice)
        {
            IQueryable<Vehicle> query = _db.Vehicles.Include(v => v.VehicleImages).Where(v => v.Status == "Available");
            if (!string.IsNullOrEmpty(brand) && brand != "Any")
            {
                query = query.Where(v => v.Brand == brand);
            }
            if (!string.IsNullOrEmpty(type) && type != "Any")
            {
                query = query.Where(v => v.Type == type);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(v => v.DailyRate <= maxPrice.Value);
            }
            var result = await query.Select(v => new
            {
                v.VehicleID,
                v.Brand,
                v.Model,
                v.Type,
                v.DailyRate,
                IsAvaliableNow = v.Status == "Available",
                PrimaryImageURL = v.VehicleImages
                   .Where(img => img.IsPrimary)
                   .Select(img => img.ImageURL)
                   .FirstOrDefault()
            }).ToListAsync();
            return Ok(result);
        }
        [HttpPost("search/date")]
        public async Task<IActionResult> SearchDateVehicle([FromBody] SearchVechileDTO searchVechile)
        {
            try
            {
                if (searchVechile.StartDate >= searchVechile.EndDate)
                {
                    var temp = searchVechile.StartDate;
                    searchVechile.StartDate = searchVechile.EndDate;
                    searchVechile.EndDate = temp;
                }
                if (searchVechile.StartDate == searchVechile.EndDate)
                    searchVechile.EndDate += TimeSpan.FromDays(1);
                var bookedVehicleIds = await _db.Bookings
                   .Where(b => (b.Status == "Pending" || b.Status == "Confirmed" || b.Status == "InProgress") &&
                        b.StartDate < searchVechile.EndDate &&
                        b.EndDate > searchVechile.StartDate)
                   .Select(b => b.VehicleID)
                   .Distinct()
                   .ToListAsync();

                var availableVehicles = await _db.Vehicles
                    .Include(v => v.VehicleImages)
                    .Where(v => !bookedVehicleIds.Contains(v.VehicleID))
                    .Where(v => v.Status == "Available" || v.Status == "Rented")
                    .Select(v => new
                    {
                        v.VehicleID,
                        v.Brand,
                        v.Model,
                        v.Type,
                        v.DailyRate,
                        IsAvaliableNow = v.Status == "Available",
                        PrimaryImageURL = v.VehicleImages
                           .Where(img => img.IsPrimary)
                           .Select(img => img.ImageURL)
                           .FirstOrDefault()
                    }).ToListAsync();
                return Ok(availableVehicles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = "An error occurred while searching for vehicles: " + ex.Message
                });
            }
        }
    }
}
