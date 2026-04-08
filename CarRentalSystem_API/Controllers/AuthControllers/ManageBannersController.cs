using CarRentalSystem_API.DTO.BannersDTO;
using CarRentalSystem_API.Function;
using CarRentalSystem_API.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AuthControllers
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
                    return BadRequest(new
                    {
                        error = "Invalid input data.",
                        Message = "Please ensure all required fields are filled out correctly and the data format is valid."
                    });
                }
                if (string.IsNullOrWhiteSpace(createBanners.Title))
                {
                    return BadRequest(new
                    {
                        error = "Title is required.",
                        Message = "The Title field is required and cannot be empty or whitespace."
                    });
                }

                if (createBanners.StartDate >= createBanners.EndDate)
                {
                    return BadRequest(new
                    {
                        error = "Invalid date range.",
                        Message = "Start date must be before end date."
                    });
                }
                if (createBanners.ImageUrl == null || createBanners.ImageUrl.Length == 0)
                {
                    return BadRequest(new
                    {
                        error = "Image is required.",
                        Message = "An image file must be uploaded for the banner."
                    });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(createBanners.ImageUrl.FileName).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                {
                    return BadRequest(new
                    {
                        error = "Invalid file type.",
                        Message = "Only image files with extensions .jpg, .jpeg, .png, or .gif are allowed."
                    });

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
                        Folder = "car_rental_system/Banners"
                    };
                    uploadFolder = cloudinary.Upload(uploadParams);
                }
                if (uploadFolder == null || string.IsNullOrEmpty(uploadFolder.SecureUrl.AbsoluteUri))
                {
                    return StatusCode(500, new
                    {
                        error = "Image upload failed.",
                        Message = "An error occurred while uploading the image to Cloudinary. Please try again later."
                    });
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
                return Ok(new
                {
                    Message = "Banner created successfully.",
                    BannerID = _db.Banners.OrderByDescending(b => b.BannersID).FirstOrDefault().BannersID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred while creating the banner.",
                    Message = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateBanner([FromForm] UpdateBannersDTO updateBanners)
        {
            try
            {
                var banner = await _db.Banners.FirstOrDefaultAsync(b => b.BannersID == updateBanners.ID);
                if (banner == null)
                {
                    return NotFound(new
                    {
                        error = "Banner not found.",
                        Message = $"No banner with ID {updateBanners.ID} exists in the database."
                    });
                }
                if (updateBanners.ImageUrl != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var ext = Path.GetExtension(updateBanners.ImageUrl.FileName).ToLowerInvariant();
                    if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
                    {
                        return BadRequest(new
                        {
                            error = "Invalid file type.",
                            Message = "Only image files with extensions .jpg, .jpeg, .png, or .gif are allowed."
                        });
                    }
                    var cloudName = _config["CloudinarySettings:CloudName"];
                    var apiKey = _config["CloudinarySettings:ApiKey"];
                    var apiSecret = _config["CloudinarySettings:ApiSecret"];

                    Account account = new Account(cloudName, apiKey, apiSecret);
                    Cloudinary cloudinary = new Cloudinary(account);
                    cloudinary.Api.Secure = true;

                    string publicID = ExtractPublicURL.ExtractPublicIDFromUrl(banner.ImageURL);
                    if (!string.IsNullOrEmpty(publicID))
                    {
                        var deletionParams = new DeletionParams(publicID);
                        var deletionResult = cloudinary.Destroy(deletionParams);
                        if (deletionResult.Result != "ok")
                        {
                            return StatusCode(500, new
                            {
                                error = "Failed to delete the image from Cloudinary.",
                                Message = "An error occurred while deleting the old image from Cloudinary. Please try again later."
                            });
                        }
                    }
                    var uploadFolder = new ImageUploadResult();
                    using (var stream = updateBanners.ImageUrl.OpenReadStream())
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(updateBanners.ImageUrl.FileName, stream),
                            Folder = "car_rental_system/Banners"
                        };
                        uploadFolder = cloudinary.Upload(uploadParams);
                    }
                    if (uploadFolder == null || string.IsNullOrEmpty(uploadFolder.SecureUrl.AbsoluteUri))
                    {
                        return StatusCode(500, new
                        {
                            error = "Image upload failed.",
                            Message = "An error occurred while uploading the image to Cloudinary. Please try again later."
                        });
                    }
                    banner.ImageURL = uploadFolder.SecureUrl.AbsoluteUri;
                }
                else
                {
                    banner.ImageURL = banner.ImageURL;
                }
                banner.Title = updateBanners.Title;
                banner.Description = string.IsNullOrEmpty(updateBanners.Description) ? updateBanners.Title : updateBanners.Description;
                banner.TargetURL = updateBanners.TargetUrl;
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    Message = "Banner updated successfully.",
                    BannerID = banner.BannersID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred while updating the banner.",
                    Message = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }
        [HttpDelete("{bannnersid}")]
        public async Task<IActionResult> DeleteBanner(int bannnersid)
        {
            try
            {
                var banner = await _db.Banners.FirstOrDefaultAsync(b => b.BannersID == bannnersid);
                if (banner == null)
                {
                    return NotFound(new
                    {
                        error = "Banner not found.",
                        Message = $"No banner with ID {bannnersid} exists in the database."
                    });
                }
                var cloudName = _config["CloudinarySettings:CloudName"];
                var apiKey = _config["CloudinarySettings:ApiKey"];
                var apiSecret = _config["CloudinarySettings:ApiSecret"];
                Account account = new Account(cloudName, apiKey, apiSecret);
                Cloudinary cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;

                string publicID = ExtractPublicURL.ExtractPublicIDFromUrl(banner.ImageURL);
                if (!string.IsNullOrEmpty(publicID))
                {
                    var deletionParams = new DeletionParams(publicID);
                    var deletionResult = cloudinary.Destroy(deletionParams);
                    if (deletionResult.Result != "ok")
                    {
                        return StatusCode(500, new
                        {
                            error = "Failed to delete the image from Cloudinary.",
                            Message = "An error occurred while deleting the image from Cloudinary. Please try again later."
                        });
                    }
                }
                _db.Banners.Remove(banner);
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    Message = "Banner deleted successfully.",
                    BannerID = bannnersid
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred while deleting the banner.",
                    Message = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }
        [HttpPost("{banners}/toggle")]
        public async Task<IActionResult> ToggleBannerStatus(int banners)
        {
            var banner = await _db.Banners.FirstOrDefaultAsync(b => b.BannersID == banners);
            if (banner == null)
            {
                return NotFound(new
                {
                    error = "Banner not found.",
                    Message = $"No banner with ID {banners} exists in the database."
                });
            }
            banner.IsActive = !banner.IsActive;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = $"Banner {(banner.IsActive ? "activated" : "deactivated")} successfully.",
                BannerID = banners,
                NewStatus = banner.IsActive ? "Active" : "Inactive"
            });
        }

    }
}