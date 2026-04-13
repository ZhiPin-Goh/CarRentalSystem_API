using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/user/promotion")]
    [Tags("User Promotion Management")]
    [Authorize(Roles = "User")]
    public class PromotionUserController : Controller
    {
        private readonly AppDbContext _db;
        public PromotionUserController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("promotions")]
        public async Task<IActionResult> GetActivePromtions()
        {
            var activePromotions = await _db.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .ToListAsync();

            return Ok(activePromotions);
        }
    }
}
