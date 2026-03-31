using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManagePromotionController : Controller
    {
        private readonly AppDbContext _db;
        public ManagePromotionController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllPromotions()
        {
            var promotions = await _db.Promotions.ToListAsync();
            return Ok(promotions);
        }
    }
}
