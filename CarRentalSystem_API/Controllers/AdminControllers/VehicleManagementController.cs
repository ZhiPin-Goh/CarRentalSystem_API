using CarRentalSystem_API.DTO.VehicleDTO;
using CarRentalSystem_API.Function;
using System.Linq;
using CarRentalSystem_API.Models;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class VehicleManagementController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public VehicleManagementController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        [HttpGet]
        public async Task<IActionResult> GetAllVehicle()
        {
            var vehicles = await _db.Vehicles.ToListAsync();
            return Ok(vehicles);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetVehicleByID(int id)
        {
            var vehicle = await _db.Vehicles.FindAsync(id);
            if (vehicle == null)
                return NotFound("Vehicle not found");
            return Ok(vehicle);
        }
        [HttpPost]
        public async Task<IActionResult> CreateVehicle([FromForm] CreateVehicleDTO vehicleDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                var existingVehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == vehicleDTO.LicensePlate);
                if (existingVehicle != null)
                    return BadRequest("A vehicle with the same license plate already exists");
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);
                if (vehicleDTO.Year < 1990 || vehicleDTO.Year > DateTime.Now.Year + 1)
                    return BadRequest("Year must be between 1990 and next year");
                if (vehicleDTO.DailyRate <= 0)
                    return BadRequest("Daily rate must be greater than zero");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extPrimary = Path.GetExtension(vehicleDTO.PrimaryImage.FileName).ToLowerInvariant();
                foreach (var item in vehicleDTO.AdditionalImages ?? new List<IFormFile>())
                {
                    var extAdditional = Path.GetExtension(item.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extAdditional))
                        return BadRequest("Only image files are allowed for additional images");
                }
                if (!allowedExtensions.Contains(extPrimary))
                    return BadRequest("Only image files are allowed for primary image");

                var cloudName = _config["CloudinarySettings:CloudName"];
                var apiKey = _config["CloudinarySettings:ApiKey"];
                var apiSecret = _config["CloudinarySettings:ApiSecret"];
                Account account = new Account(cloudName, apiKey, apiSecret);
                Cloudinary cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;

                var imageSave = new List<VehicleImage>();

                var uploadFolderPrimary = new ImageUploadResult();
                using (var streamprimary = vehicleDTO.PrimaryImage.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(vehicleDTO.PrimaryImage.FileName, streamprimary),
                        Folder = "Car Rental System/Vehicles"
                    };
                    uploadFolderPrimary = cloudinary.Upload(uploadParams);
                    if (uploadFolderPrimary.Error != null)
                        return StatusCode(500, "Failed to upload primary image: " + uploadFolderPrimary.Error.Message);
                    imageSave.Add(new VehicleImage
                    {
                        ImageURL = uploadFolderPrimary.SecureUrl.AbsoluteUri,
                        IsPrimary = true
                    });
                }

                foreach (var item in vehicleDTO.AdditionalImages ?? new List<IFormFile>())
                {
                    var uploadFolderAdditional = new ImageUploadResult();
                    using (var streamAdditional = item.OpenReadStream())
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(item.FileName, streamAdditional),
                            Folder = "Car Rental System/Vehicles"
                        };
                        uploadFolderAdditional = cloudinary.Upload(uploadParams);
                        if (uploadFolderAdditional.Error != null)
                            return StatusCode(500, "Failed to upload additional image: " + item.FileName);
                    }

                    imageSave.Add(new VehicleImage
                    {
                        ImageURL = uploadFolderAdditional.SecureUrl.AbsoluteUri,
                        IsPrimary = false
                    });
                }
                var vehicle = new Vehicle
                {
                    Brand = vehicleDTO.Brand,
                    Model = vehicleDTO.Model,
                    Year = vehicleDTO.Year,
                    LicensePlate = vehicleDTO.LicensePlate,
                    DailyRate = vehicleDTO.DailyRate,
                    Status = "Available",
                    Type = vehicleDTO.Type,
                    FuelType = vehicleDTO.FuelType,
                    SpecsInfo = vehicleDTO.SpecsInfo ?? string.Empty,
                    VehicleImages = imageSave
                };
                _db.Vehicles.Add(vehicle);
                await _db.SaveChangesAsync();
                return Ok("Vehicle created successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while creating the vehicle: " + ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateVehicleInformation([FromBody] UpdateVehicleInformationDTO updateVehicle)
        {
            try
            {
                var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleID == updateVehicle.VechicleID);
                if (vehicle == null)
                    return NotFound("Vehicle not found");
                if (updateVehicle.DailyRate < 0)
                    return BadRequest("Daily rate must be greater than or equal to zero");
                if (updateVehicle.Year < 1990 || updateVehicle.Year > DateTime.Now.Year + 1)
                    return BadRequest("Year must be between 1990 and next year");

                _db.Entry(vehicle).CurrentValues.SetValues(updateVehicle);
                await _db.SaveChangesAsync();
                return Ok("Vehicle information updated successfully");

            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the vehicle: " + ex.Message);
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateVehicleImage([FromForm] UpdateVehicleImageDTO updateVehicle)
        {
            try
            {
                var vehicle = await _db.Vehicles.Include(v => v.VehicleImages).FirstOrDefaultAsync(v => v.VehicleID == updateVehicle.VehicleID);
                if (vehicle == null)
                    return NotFound("Vehicle not found");

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };

                var cloudName = _config["CloudinarySettings:CloudName"];
                var apiKey = _config["CloudinarySettings:ApiKey"];
                var apiSecret = _config["CloudinarySettings:ApiSecret"];
                Account account = new Account(cloudName, apiKey, apiSecret);
                Cloudinary cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;

                if (updateVehicle.PrimaryImage != null)
                {
                    var extPrimary = Path.GetExtension(updateVehicle.PrimaryImage.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extPrimary) || string.IsNullOrEmpty(extPrimary))
                        return BadRequest("Only image files are allowed for primary image");

                    string publicIDPrimary = ExtractPublicURL.ExtractPublicIDFromUrl(updateVehicle.PrimaryImage.FileName);
                    if (!string.IsNullOrEmpty(publicIDPrimary))
                    {
                        var deletionParams = new DeletionParams(publicIDPrimary);
                        var deletionResult = cloudinary.Destroy(deletionParams);
                        if (deletionResult.Error != null)
                            return StatusCode(500, "Failed to delete existing primary image: " + deletionResult.Error.Message);
                    }
                    var uploadFolderPrimary = new ImageUploadResult();
                    using (var streamprimary = updateVehicle.PrimaryImage.OpenReadStream())
                    {
                        var uploadParams = new ImageUploadParams()
                        {
                            File = new FileDescription(updateVehicle.PrimaryImage.FileName, streamprimary),
                            Folder = "CarRentalSystem/Vehicles"
                        };
                        uploadFolderPrimary = cloudinary.Upload(uploadParams);
                        if (uploadFolderPrimary.Error != null)
                            return StatusCode(500, "Failed to upload primary image: " + uploadFolderPrimary.Error.Message);
                    }
                    var existingPrimaryImage = vehicle.VehicleImages.FirstOrDefault(img => img.IsPrimary);
                    if (existingPrimaryImage != null)
                    {
                        existingPrimaryImage.ImageURL = uploadFolderPrimary.SecureUrl.AbsoluteUri;
                    }
                    else
                    {
                        vehicle.VehicleImages.Add(new VehicleImage
                        {
                            ImageURL = uploadFolderPrimary.SecureUrl.AbsoluteUri,
                            IsPrimary = true
                        });
                    }
                }
                if (updateVehicle.AdditionalImages != null && updateVehicle.AdditionalImages.Count > 0)
                {
                    foreach (var image in updateVehicle.AdditionalImages)
                    {
                        var extAdditional = Path.GetExtension(image.FileName).ToLowerInvariant();
                        if (!allowedExtensions.Contains(extAdditional) || string.IsNullOrEmpty(extAdditional))
                            return BadRequest("Only image files are allowed for additional images");
                        var uploadFolderAdditional = new ImageUploadResult();
                        using (var streamAdditional = image.OpenReadStream())
                        {
                            var uploadParams = new ImageUploadParams()
                            {
                                File = new FileDescription(image.FileName, streamAdditional),
                                Folder = "CarRentalSystem/Vehicles"
                            };
                            uploadFolderAdditional = cloudinary.Upload(uploadParams);
                            if (uploadFolderAdditional.Error != null)
                                return StatusCode(500, "Failed to upload additional image: " + image.FileName + " - " + uploadFolderAdditional.Error.Message);
                        }
                        vehicle.VehicleImages.Add(new VehicleImage
                        {
                            ImageURL = uploadFolderAdditional.SecureUrl.AbsoluteUri,
                            IsPrimary = false
                        });
                    }
                }
                await _db.SaveChangesAsync();
                return Ok("Vehicle images updated successfully");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "An error occurred while updating the vehicle images: " + ex.Message);
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.VehicleID == id);
            if (vehicle == null)
                return NotFound("Vehicle not found");

            if (vehicle.Status != "Available")
                return BadRequest("Only vehicles with 'Available' status can be deleted");

            var existingBookings = await _db.Bookings.Where(b => b.VehicleID == id).ToListAsync();
            if (existingBookings.Where(x => x.StartDate > DateTime.Now || x.EndDate > DateTime.Now).Count() > 0)
                return BadRequest("Cannot delete vehicle with active or upcoming bookings");

            var cloudName = _config["CloudinarySettings:CloudName"];
            var apiKey = _config["CloudinarySettings:ApiKey"];
            var apiSecret = _config["CloudinarySettings:ApiSecret"];
            Account account = new Account(cloudName, apiKey, apiSecret);
            Cloudinary cloudinary = new Cloudinary(account);
            cloudinary.Api.Secure = true;

            if (vehicle.VehicleImages != null && vehicle.VehicleImages.Any())
            {
                foreach (var image in vehicle.VehicleImages)
                {
                    string publicID = ExtractPublicURL.ExtractPublicIDFromUrl(image.ImageURL);
                    if (!string.IsNullOrEmpty(publicID))
                    {
                        var deletionParams = new DeletionParams(publicID);
                        var deletionResult = await cloudinary.DestroyAsync(deletionParams);

                        if (deletionResult.Error != null)
                        {
                            Console.WriteLine($"Failed to delete image {image.ImageURL}: {deletionResult.Error.Message}");
                        }
                    }
                }
            }
            _db.Vehicles.Remove(vehicle);
            await _db.SaveChangesAsync();
            return Ok("Vehicle deleted successfully");
        }
        [HttpPost("{id}")]
        public async Task<IActionResult> InAvailableVehicle(int id)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.VehicleID == id);
            if (vehicle == null)
                return NotFound("Vehicle not found");
            if (vehicle.Status == "Rented")
                return BadRequest("Cannot mark vehicle as unavailable while it is currently rented");
            if (vehicle.Status == "Maintenance")
                return BadRequest("Vehicle is already marked as unavailable for maintenance");

            bool hasActiveBookings = await _db.Bookings.AnyAsync(b => b.VehicleID == id && (b.StartDate > DateTime.Now || b.EndDate > DateTime.Now));
            if (hasActiveBookings)
                return BadRequest("Cannot mark vehicle as unavailable while it has active or upcoming bookings");

            vehicle.Status = "Unavailable";
            await _db.SaveChangesAsync();
            return Ok("Vehicle marked as unavailable successfully");
        }
        [HttpPost("{id}")]
        public async Task<IActionResult> AvailableVehicle(int id)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.VehicleID == id);
            if (vehicle == null)
                return NotFound("Vehicle not found");
            if (vehicle.Status == "Rented")
                return BadRequest("Cannot mark vehicle as available while it is currently rented");
            if (vehicle.Status == "Available")
                return BadRequest("Vehicle is already marked as available");
            vehicle.Status = "Available";
            await _db.SaveChangesAsync();
            return Ok("Vehicle marked as available successfully");
        }
    }
}
