using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [Route("api/user/banners")]
    [ApiController]
    [Tags("User Banners")]
    [Authorize(Roles = "User")]
    public class BannersUserController : Controller
    {
        private readonly AppDbContext _db;
        public BannersUserController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet ("active")]
        public async Task<IActionResult> GetActiveBanners()
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
