using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class BannersController : Controller
    {
        private readonly AppDbContext _db;
        public BannersController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetBanners()
        {
            var banners = await _db.Banners
            .Where(x => x.StartDate <= DateTime.UtcNow && x.EndDate >= DateTime.UtcNow && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new
            {
                x.Title,
                x.Description,
                x.ImageURL,
                x.TargetURL,
            }).ToListAsync();
            return Ok(banners);
        }
    }
}
