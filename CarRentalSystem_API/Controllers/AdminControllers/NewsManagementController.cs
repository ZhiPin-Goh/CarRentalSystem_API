using CarRentalSystem_API.DTO.NewsDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin News Management")]
    public class NewsManagementController : Controller
    {
      private readonly AppDbContext _db;
        public NewsManagementController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetPublishedNews()
        {
            var news = await _db.News.Where(x => x.IsPublished == true)
                .Select(x => new
                {
                    x.NewsID,
                    x.Title,
                    x.Content,
                    x.PublishedDate
                }).ToListAsync();

            return Ok(news);
        }
        [HttpGet ("news")]
        public async Task<IActionResult> GetAllNews()
        {
            var news = await _db.News
                .ToListAsync();
            return Ok(news);
        }
        [HttpPost ("createnews")]
        public async Task<IActionResult> CreateNews([FromBody] CreateNewsDTO createNews)
        {
            if (string.IsNullOrWhiteSpace(createNews.Title) || string.IsNullOrWhiteSpace(createNews.Content))
            {
                return BadRequest(new
                {
                    error = "Invalid Input",
                    message = "Title and content are required."
                });
            }
            if (createNews.CoverImageURL == null)
                createNews.CoverImageURL = "https://example.com/default-cover.jpg";

            var news = new News
            {
                Title = createNews.Title,
                Summary = createNews.Summary,
                Content = createNews.Content,
                CoverImageURL = createNews.CoverImageURL,
                Author = createNews.Author,
                PublishedDate = DateTime.UtcNow,
                IsPublished = true
            };
            await _db.News.AddAsync(news);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "News created successfully.",
                NewsID = news.NewsID
            });
        }
        [HttpPost ("updatenews")]
        public async Task<IActionResult> UpdateNews([FromBody] UpdateNewDTO updateNew)
        {
            var existingNews = await _db.News.FirstOrDefaultAsync(n => n.NewsID == updateNew.NewsID);
            if (existingNews == null)
                return NotFound(new
                {
                    error = "News Not Found",
                    message = $"News with ID {updateNew.NewsID} not found."
                });
            if (string.IsNullOrWhiteSpace(updateNew.Title) || string.IsNullOrWhiteSpace(updateNew.Content))
            {
                return BadRequest(new
                {
                    error = "Invalid Input",
                    message = "Title and content are required."
                });
            }
            _db.Entry(existingNews).CurrentValues.SetValues(updateNew);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "News updated successfully.",
                NewsID = existingNews.NewsID
            });
        }
        [HttpDelete("deletenews/{newsid}")]
        public async Task<IActionResult> DeleteNews(int newsid)
        {
            var existingNews = await _db.News.FirstOrDefaultAsync(n => n.NewsID == newsid);
            if (existingNews == null)
                return NotFound(new
                {
                    error = "News Not Found",
                    message = $"News with ID {newsid} not found."
                });
            if (existingNews.IsPublished)
                return BadRequest(new
                {
                    error = "Cannot Delete Published News",
                    message = "Only unpublished news can be deleted."
                });
            _db.News.Remove(existingNews);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "News deleted successfully.",
                NewsID = existingNews.NewsID
            });
        }
        [HttpPost("togglenewsstatus/{id}")]
        public async Task<IActionResult> ToggleNewsStatus(int id)
        {
            var existingNews = await _db.News.FirstOrDefaultAsync(n => n.NewsID == id);
            if (existingNews == null)
                return NotFound(new
                {
                    error = "News Not Found",
                    message = $"News with ID {id} not found."
                });
            existingNews.IsPublished = !existingNews.IsPublished;
            if (existingNews.IsPublished)
                existingNews.PublishedDate = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = $"News {(existingNews.IsPublished ? "published" : "unpublished")} successfully.",
                NewsID = existingNews.NewsID,
                IsPublished = existingNews.IsPublished
            });
        }
    }
}
