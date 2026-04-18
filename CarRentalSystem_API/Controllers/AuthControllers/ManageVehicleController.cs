using CarRentalSystem_API.DTO.VehicleDTO;
using CarRentalSystem_API.Function;
using CarRentalSystem_API.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Linq;
using static Org.BouncyCastle.Asn1.Cmp.Challenge;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Tags("Auth Vehicle Management")]
    public class ManageVehicleController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public ManageVehicleController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVehicle()
        {
            var vehicles = await _db.Vehicles
                .Include(x => x.VehicleImages)
                .Select(v => new
                {
                    v.VehicleID,
                    v.Brand,
                    v.Model,
                    v.Year,
                    v.LicensePlate,
                    v.DailyRate,
                    v.Status,
                    v.Type,
                    v.FuelType,
                    v.SpecsInfo,
                    PrimaryImageURL = v.VehicleImages
                   .Where(img => img.IsPrimary)
                   .Select(img => img.ImageURL)
                   .FirstOrDefault()
                }).ToListAsync();
            return Ok(vehicles);
        }
        [HttpGet]
        public async Task<IActionResult> GetHomePageVehicles()
        {
            var vehicles = await _db.Vehicles
                .Where(v => v.Status == "Available" || v.Status == "Rented")
                .Include(v => v.VehicleImages)
                .Take(6)
                .Select(v => new
                {
                    v.VehicleID,
                    v.Brand,
                    v.Model,
                    v.Type,
                    v.DailyRate,
                    IsAvaliableNow = v.Status == "Available",
                    PrimaryImageURL = v.VehicleImages
                   .Where(img => img.IsPrimary)
                   .Select(img => img.ImageURL)
                   .FirstOrDefault()
                })
                .ToListAsync();
           
            return Ok(vehicles);    
        }
        [HttpGet ("{vehicleID}")]
        public async Task<IActionResult> GetVehicle(int vehicleID)
        {
            var vehicle = await _db.Vehicles
                .Include(v => v.VehicleImages)
                .Where(v => v.VehicleID == vehicleID)
                .Select(v => new
                {
                    v.VehicleID,
                    v.Brand,
                    v.Model,
                    v.Year,
                    v.LicensePlate,
                    v.DailyRate,
                    v.Status,
                    v.Type,
                    v.FuelType,
                    v.SpecsInfo,
                    PrimaryImageURL = v.VehicleImages.FirstOrDefault(img => img.IsPrimary).ImageURL,
                    AdditionalImageURLs = v.VehicleImages.Where(img => !img.IsPrimary).Select(img => img.ImageURL).ToList()
                }).FirstOrDefaultAsync();
            if (vehicle == null)
                return NotFound(new
                {
                    error = "Vehicle Not Found",
                    message = $"No vehicle found with ID {vehicleID}. Please check the ID and try again."
                });
            return Ok(vehicle);
        }
        [HttpGet]
        public async Task<IActionResult> SearchTypeSearch([FromQuery] string? brand, [FromQuery] string? type, [FromQuery] decimal? maxPrice)
        {
            IQueryable<Vehicle> query = _db.Vehicles.Include(v => v.VehicleImages).Where(v => v.Status == "Available");
            if (!string.IsNullOrEmpty(brand) && brand != "Any")
            {
                query = query.Where(v => v.Brand == brand);
            }
            if (!string.IsNullOrEmpty(type) && type != "Any")
            {
                query = query.Where(v => v.Type == type);
            }
            if (maxPrice.HasValue && maxPrice.Value > 0)
            {
                query = query.Where(v => v.DailyRate <= maxPrice.Value);
            }
            var result = await query.Select(v => new
            {
                v.VehicleID,
                v.Brand,
                v.Model,
                v.Type,
                v.DailyRate,
                IsAvaliableNow = v.Status == "Available",
                PrimaryImageURL = v.VehicleImages
                   .Where(img => img.IsPrimary)
                   .Select(img => img.ImageURL)
                   .FirstOrDefault()
            }).ToListAsync();
            return Ok(result);
        }
        [HttpGet]
        public async Task<IActionResult> SearchVehicle(
           [FromQuery] string? brand,
           [FromQuery] string? type,
           [FromQuery] decimal? maxPrice,
           [FromQuery] DateTime? startDate,
           [FromQuery] DateTime? endDate)
        {
            try
            {
                IQueryable<Vehicle> query = _db.Vehicles.Include(v => v.VehicleImages);
                if (startDate.HasValue && endDate.HasValue)
                {
                    if (startDate.Value >= endDate.Value)
                    {
                        var temp = startDate.Value;
                        startDate = endDate;
                        endDate = temp;
                    }
                    if (startDate.Value == endDate.Value)
                        endDate = endDate.Value.AddDays(1);
                    var bookedVehicleIds = await _db.Bookings
                       .Where(b => (b.Status == "Pending" || b.Status == "Confirmed" || b.Status == "InProgress") &&
                            b.StartDate < endDate.Value &&
                            b.EndDate > startDate.Value)
                       .Select(b => b.VehicleID)
                       .Distinct()
                       .ToListAsync();
                    query = query.Where(v => !bookedVehicleIds.Contains(v.VehicleID));
                }
                else
                {
                    query = query.Where(v => v.Status == "Available" || v.Status == "Rented");
                }
                if (!string.IsNullOrEmpty(brand) && brand != "Any")
                {
                    query = query.Where(v => v.Brand == brand);
                }
                if (!string.IsNullOrEmpty(type) && type != "Any")
                {
                    query = query.Where(v => v.Type == type);
                }
                if (maxPrice.HasValue && maxPrice.Value > 0)
                {
                    query = query.Where(v => v.DailyRate <= maxPrice.Value);
                }
                var result = await query.Select(v => new
                {
                    VehicleID = v.VehicleID,
                    Brand = v.Brand,
                    Model = v.Model,
                    Type = v.Type,
                    DailyRate = v.DailyRate,
                    IsAvaliableNow = v.Status == "Available",
                    PrimaryImageURL = v.VehicleImages
               .Where(img => img.IsPrimary)
               .Select(img => img.ImageURL)
               .FirstOrDefault()
                }).ToListAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = "An error occurred while searching for vehicles: " + ex.Message
                });
            }
        }
        [HttpGet("{licensePlate}")]
        public async Task<IActionResult> SearchVehicle(string licensePlate)
        {
            var vehicle = await _db.Vehicles
                .Include(v => v.VehicleImages)
                .Where(v => v.LicensePlate == licensePlate)
                .Select(v => new
                {
                    v.VehicleID,
                    v.Brand,
                    v.Model,
                    v.Year,
                    v.LicensePlate,
                    v.DailyRate,
                    v.Status,
                    v.Type,
                    v.FuelType,
                    v.SpecsInfo,
                    PrimaryImageURL = v.VehicleImages.FirstOrDefault(img => img.IsPrimary).ImageURL
                }).FirstOrDefaultAsync();
            if (vehicle == null)
                return NotFound(new
                {
                    error = "Vehicle Not Found",
                    message = $"No vehicle found with license plate {licensePlate}. Please check the license plate and try again."
                });
            return Ok(vehicle);
        }
        [HttpPost]
        public async Task<IActionResult> SearchDateVehicle([FromBody] SearchVechileDTO searchVechile)
        {
            try
            {
                if (searchVechile.StartDate >= searchVechile.EndDate)
                {
                    var temp = searchVechile.StartDate;
                    searchVechile.StartDate = searchVechile.EndDate;
                    searchVechile.EndDate = temp;
                }
                if (searchVechile.StartDate == searchVechile.EndDate)
                    searchVechile.EndDate += TimeSpan.FromDays(1);
                var bookedVehicleIds = await _db.Bookings
                   .Where(b => (b.Status == "Pending" || b.Status == "Confirmed" || b.Status == "InProgress") &&
                        b.StartDate < searchVechile.EndDate &&
                        b.EndDate > searchVechile.StartDate)
                   .Select(b => b.VehicleID)
                   .Distinct()
                   .ToListAsync();

                var availableVehicles = await _db.Vehicles
                    .Include(v => v.VehicleImages)
                    .Where(v => !bookedVehicleIds.Contains(v.VehicleID))
                    .Where(v => v.Status == "Available")
                    .Select(v => new
                    {
                        v.VehicleID,
                        v.Brand,
                        v.Model,
                        v.Type,
                        v.DailyRate,
                        IsAvaliableNow = v.Status == "Available",
                        PrimaryImageURL = v.VehicleImages.FirstOrDefault(img => img.IsPrimary).ImageURL
                    }).ToListAsync();
                return Ok(availableVehicles);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = "An error occurred while searching for vehicles: " + ex.Message
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateVehicle([FromForm] CreateVehicleDTO vehicleDTO)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(new
                    {
                        error = "Validation Error",
                        message = "Please ensure all required fields are filled out correctly."
                    });

                var existingVehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.LicensePlate == vehicleDTO.LicensePlate);
                if (existingVehicle != null)
                    return BadRequest(new
                    {
                        error = "Duplicate License Plate",
                        message = $"A vehicle with license plate {vehicleDTO.LicensePlate} already exists."
                    });
                if (vehicleDTO.Year < 1990 || vehicleDTO.Year > DateTime.Now.Year + 1)
                    return BadRequest(new
                    {
                        error = "Invalid Year",
                        message = $"Year must be between 1990 and {DateTime.Now.Year + 1}."
                    });
                if (vehicleDTO.DailyRate <= 0)
                    return BadRequest(new
                    {
                        error = "Invalid Daily Rate",
                        message = "Daily rate must be greater than zero."
                    });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extPrimary = Path.GetExtension(vehicleDTO.PrimaryImage.FileName).ToLowerInvariant();
                foreach (var item in vehicleDTO.AdditionalImages ?? new List<IFormFile>())
                {
                    var extAdditional = Path.GetExtension(item.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extAdditional))
                        return BadRequest(new
                        {
                            error = "Invalid File Type",
                            message = $"Only image files are allowed for additional images. Invalid file: {item.FileName}"
                        });
                }
                if (!allowedExtensions.Contains(extPrimary))
                    return BadRequest(new
                    {
                        error = "Invalid File Type",
                        message = "Only image files are allowed for primary image."
                    });

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
                        Folder = "car_rental_system/Vehicles"
                    };
                    uploadFolderPrimary = cloudinary.Upload(uploadParams);
                    if (uploadFolderPrimary.Error != null)
                        return StatusCode(500, new
                        {
                            error = "Image Upload Failed",
                            message = "Failed to upload primary image: " + uploadFolderPrimary.Error.Message
                        });
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
                            Folder = "car_rental_system/Vehicles"
                        };
                        uploadFolderAdditional = cloudinary.Upload(uploadParams);
                        if (uploadFolderAdditional.Error != null)
                            return StatusCode(500, new
                            {
                                error = "Image Upload Failed",
                                message = "Failed to upload additional image: " + item.FileName + " - " + uploadFolderAdditional.Error.Message
                            });
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
                return Ok(new
                {
                    message = "Vehicle created successfully.",
                    VehicleID = vehicle.VehicleID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = "An error occurred while creating the vehicle: " + ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateVehicleInformation([FromBody] UpdateVehicleInformationDTO updateVehicle)
        {
            try
            {
                var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleID == updateVehicle.VechicleID);
                if (vehicle == null)
                    return NotFound(new
                    {
                        error = "Vehicle Not Found",
                        message = $"Vehicle with ID {updateVehicle.VechicleID} not found."
                    });
                if (updateVehicle.DailyRate < 0)
                {
                    return BadRequest(new
                    {
                        error = "Invalid Daily Rate",
                        message = "Daily rate must be greater than or equal to zero."
                    });
                }
                if (updateVehicle.Year < 1990 || updateVehicle.Year > DateTime.Now.Year + 1)
                    return BadRequest(new
                    {
                        error = "Invalid Year",
                        message = $"Year must be between 1990 and {DateTime.Now.Year + 1}."
                    });

                _db.Entry(vehicle).CurrentValues.SetValues(updateVehicle);
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    message = "Vehicle information updated successfully.",
                    VehicleID = vehicle.VehicleID
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = "An error occurred while updating the vehicle information: " + ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateExpiryDate([FromBody] UpdateExpiryDTO updateExpiry)
        {
            try
            {
                var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleID == updateExpiry.VehicleID);
                if (vehicle == null)
                    return NotFound(new
                    {
                        error = "Vehicle Not Found",
                        message = $"Vehicle with ID {updateExpiry.VehicleID} not found."
                    });
                if (updateExpiry.RoadTaxExpiryDate.HasValue && updateExpiry.RoadTaxExpiryDate.Value < DateTime.Now)
                    return BadRequest(new
                    {
                        error = "Invalid Road Tax Expiry Date",
                        message = "Road tax expiry date cannot be in the past."
                    });
                if (updateExpiry.InsuranceExpiryDate.HasValue && updateExpiry.InsuranceExpiryDate.Value < DateTime.Now)
                    return BadRequest(new
                    {
                        error = "Invalid Insurance Expiry Date",
                        message = "Insurance expiry date cannot be in the past."
                    });
                vehicle.RoadTaxExpiryDate = updateExpiry.RoadTaxExpiryDate;
                vehicle.InsuranceExpiryDate = updateExpiry.InsuranceExpiryDate;
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    message = "Vehicle expiry dates updated successfully.",
                    VehicleID = vehicle.VehicleID
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = "An error occurred while updating the vehicle expiry dates: " + ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> UpdateVehicleImage([FromForm] UpdateVehicleImageDTO updateVehicle)
        {
            try
            {
                var vehicle = await _db.Vehicles.Include(v => v.VehicleImages).FirstOrDefaultAsync(v => v.VehicleID == updateVehicle.VehicleID);
                if (vehicle == null)
                    return NotFound(new
                    {
                        error = "Vehicle Not Found",
                        message = $"Vehicle with ID {updateVehicle.VehicleID} not found."
                    });

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
                        return BadRequest(new
                        {
                            error = "Invalid File Type",
                            message = "Only image files are allowed for primary image."
                        });

                    string publicIDPrimary = ExtractPublicURL.ExtractPublicIDFromUrl(updateVehicle.PrimaryImage.FileName);
                    if (!string.IsNullOrEmpty(publicIDPrimary))
                    {
                        var deletionParams = new DeletionParams(publicIDPrimary);
                        var deletionResult = cloudinary.Destroy(deletionParams);
                        if (deletionResult.Error != null)
                            return StatusCode(500, new
                            {
                                error = "Image Deletion Failed",
                                message = "Failed to delete the old primary image from Cloudinary: " + deletionResult.Error.Message
                            });
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
                            return StatusCode(500, new
                            {
                                error = "Image Upload Failed",
                                message = "Failed to upload primary image: " + uploadFolderPrimary.Error.Message
                            });
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
                                return StatusCode(500, new
                                {
                                    error = "Image Upload Failed",
                                    message = "Failed to upload additional image: " + image.FileName + " - " + uploadFolderAdditional.Error.Message
                                });
                        }
                        vehicle.VehicleImages.Add(new VehicleImage
                        {
                            ImageURL = uploadFolderAdditional.SecureUrl.AbsoluteUri,
                            IsPrimary = false
                        });
                    }
                }
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    message = "Vehicle images updated successfully.",
                    VehicleID = vehicle.VehicleID
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = "An error occurred while updating the vehicle images: " + ex.Message
                });
            }
        }
        [HttpDelete("{id}/delete")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.VehicleID == id);
            if (vehicle == null)
                return NotFound(new
                {
                    error = "Vehicle Not Found",
                    message = $"Vehicle with ID {id} not found."
                });

            if (vehicle.Status != "Available")
                return BadRequest(new
                {
                    error = "Invalid Vehicle Status",
                    message = "Only vehicles with status 'Available' can be deleted."
                });

            var existingBookings = await _db.Bookings.Where(b => b.VehicleID == id).ToListAsync();
            if (existingBookings.Where(x => x.StartDate > DateTime.Now || x.EndDate > DateTime.Now).Count() > 0)
                return BadRequest(new
                {
                    error = "Active Bookings Exist",
                    message = "Cannot delete vehicle with active or upcoming bookings."
                });

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
            return Ok(new
            {
                message = "Vehicle deleted successfully.",
                VehicleID = id
            });
        }
        [HttpPost("{id}/toggle-status")]
        public async Task<IActionResult> ToggleVehicleStatus(int id)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.VehicleID == id);
            if (vehicle == null)
                return NotFound(new
                {
                    error = "Vehicle Not Found",
                    message = $"Vehicle with ID {id} not found."
                });
            if (vehicle.Status != "Available" && vehicle.Status != "Unavailable")
                return BadRequest(new
                {
                    error = "Invalid Vehicle Status",
                    message = "Vehicle status must be either 'Available' or 'Unavailable'."
                });
            if (vehicle.Status == "Available")
            {
                var existingBookings = await _db.Bookings.Where(b => b.VehicleID == id).ToListAsync();
                if (existingBookings.Where(x => x.StartDate > DateTime.Now || x.EndDate > DateTime.Now).Count() > 0)
                    return BadRequest(new
                    {
                        error = "Active Bookings Exist",
                        message = "Cannot set vehicle to 'Unavailable' with active or upcoming bookings."
                    });
                vehicle.Status = "Unavailable";
            }
            else
            {
                vehicle.Status = "Available";
            }
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = $"Vehicle status toggled successfully. New status: {vehicle.Status}",
                VehicleID = vehicle.VehicleID
            });
        }
    }
}
