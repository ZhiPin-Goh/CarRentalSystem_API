using CarRentalSystem_API.DTO.BannersDTO;
using CarRentalSystem_API.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManageBannersController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public ManageBannersController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        // This is used to get all the banners for the home page, it will return only the active banners that are within the start and end date range
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
        //This is admin only, it will return all the banners regardless of the active status and date range
        [HttpGet]
        public async Task<IActionResult> GetAllBanners()
        {
            var banners = await _db.Banners.ToListAsync();
            return Ok(banners);
        }
        [HttpPost]
        public async Task<IActionResult> CreateBanner([FromForm] CreateBannersDTO createBanners)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }
                if (string.IsNullOrWhiteSpace(createBanners.Title))
                    return BadRequest("Title is required.");

                if (createBanners.StartDate >= createBanners.EndDate)
                    return BadRequest("Start date must be before end date.");
                if (createBanners.ImageUrl == null || createBanners.ImageUrl.Length == 0)
                    return BadRequest("Image is required.");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(createBanners.ImageUrl.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    return BadRequest("Invalid file type. Only image files are allowed.");
                }
                var cloudName = _config["CloudinarySettings:CloudName"];
                var apiKey = _config["CloudinarySettings:ApiKey"];
                var apiSecret = _config["CloudinarySettings:ApiSecret"];

                Account account = new Account(cloudName, apiKey, apiSecret);
                Cloudinary cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;

                var uploadFolder = new ImageUploadResult();
                using (var stream = createBanners.ImageUrl.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(createBanners.ImageUrl.FileName, stream),
                        Folder = "Car Rental System/Banners"
                    };
                    uploadFolder = cloudinary.Upload(uploadParams);
                }
                if (uploadFolder == null || string.IsNullOrEmpty(uploadFolder.SecureUrl.AbsoluteUri))
                {
                    return StatusCode(500, "Image upload failed.");
                }
                string cloudImageUrl = uploadFolder.SecureUrl.AbsoluteUri;
                _db.Banners.Add(new Banners
                {
                    Title = createBanners.Title,
                    Description = string.IsNullOrEmpty(createBanners.Description) ? createBanners.Title : createBanners.Description,
                    ImageURL = cloudImageUrl,
                    TargetURL = createBanners.TargetUrl,
                    SortOrder = createBanners.SortOrder,
                    StartDate = createBanners.StartDate,
                    EndDate = createBanners.EndDate
                });
                await _db.SaveChangesAsync();
                return Ok("Banner created successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while creating the banner: {ex.Message}");
            }
        }
    }
}