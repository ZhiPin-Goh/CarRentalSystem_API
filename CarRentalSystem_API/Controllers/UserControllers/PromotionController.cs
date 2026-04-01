using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PromotionController : Controller
    {
        private readonly AppDbContext _db;
        public PromotionController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetActivePromotions()
        {
            var promotions = await _db.Promotions
                .Where(p => p.IsActive && p.StartDate <= DateTime.UtcNow && p.EndDate >= DateTime.UtcNow)
                .ToListAsync();
            return Ok(promotions);
        }
    }
}
