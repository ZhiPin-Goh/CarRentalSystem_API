using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class VehicleController : Controller
    {
        private readonly AppDbContext _db;
        public VehicleController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllVehicles()
        {
            var vehicles = await _db.Vehicles.ToListAsync();
            return Ok(vehicles);

        }
    }
}
