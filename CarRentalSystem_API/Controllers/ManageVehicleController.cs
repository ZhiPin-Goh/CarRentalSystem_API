using CarRentalSystem_API.DTO.VehicleDTO;
using CarRentalSystem_API.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CarRentalSystem_API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManageVehicleController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public ManageVehicleController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVehicle()
        {
            var vehicles = await _db.Vehicles.ToListAsync();
            return Ok(vehicles);
        }
      
    }
}
