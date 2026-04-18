using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/user/news")]
    [Tags("User News Management")]
    [Authorize(Roles = "User")]
    public class NewsUserController : Controller
    {
        private readonly AppDbContext _db;
        public NewsUserController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("news")]
        public async Task<IActionResult> GetPublishedNews()
        {
            var news = await _db.News.Where(x => x.IsPublished == true)
                .Select(x => new
                {
                    x.NewsID,
                    x.Title,
                    x.Content,
                    x.PublishedDate,
                    x.Summary,
                    x.CoverImageURL,
                    x.Author
                }).ToListAsync();

            return Ok(news);
        }
    }
}
