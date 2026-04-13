using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/user/delivery")]
    [Tags("User Delivery Management")]
    [Authorize(Roles = "User")]
    public class DeliveryUserController : Controller
    {
        private readonly AppDbContext _db;
        public DeliveryUserController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("deliveryareas")]
        public async Task<IActionResult> GetActiveArea()
        {
            var activeAreas = await _db.DeliveryAreas
                .Where(a => a.IsActive)
                .Select(a => new
                {
                    a.AreaID,
                    a.AreaName,
                    a.Fee
                })
                .ToListAsync();
            if (activeAreas == null || activeAreas.Count == 0)
            {
                return NotFound(new
                {
                    error = "No active delivery areas found.",
                    message = "No active delivery areas are found in the system."
                });
            }
            return Ok(activeAreas);
        }
    }
}
