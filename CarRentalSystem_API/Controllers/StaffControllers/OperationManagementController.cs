using CarRentalSystem_API.DTO.HandoverReportDTO;
using CarRentalSystem_API.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRentalSystem_API.Controllers.StaffControllers
{
    [Route("api/staff/handover")]
    [ApiController]
    [Tags("Staff Operation Management")]
    [Authorize(Roles = "Staff")]
    public class OperationManagementController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public OperationManagementController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        // Report the vehicle condition
        [HttpPost("check-out")] // Start of rental - Check-Out
        public async Task<IActionResult> SubmitCheckOut([FromForm] CreateReportDTO createReport)
        {
            return await ProcessHandover(createReport, "Check-Out");
        }
        [HttpPost("check-in")] // End of rental - Check-In
        public async Task<IActionResult> SubmitCheckIn([FromForm] CreateReportDTO createReport)
        {
            return await ProcessHandover(createReport, "return");
        }

        private async Task<IActionResult> ProcessHandover(CreateReportDTO createReport, string reportType)
        {
            if (!ModelState.IsValid)
                return BadRequest(new
                {
                    error = "Invalid input data.",
                    message = "Please ensure all required fields are filled correctly."
                });
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userID == 0)
                return Unauthorized(new
                {
                    error = "User not authenticated.",
                    message = "Please log in to submit a handover report."
                });

            var booking = await _db.Bookings.FindAsync(createReport.BookingID);
            if (booking == null)
                return NotFound(new
                {
                    error = "Booking not found.",
                    message = $"No booking found with ID {createReport.BookingID}."
                });
            var vehicle = await _db.Vehicles.FindAsync(booking.VehicleID);
            if (vehicle == null)
                return NotFound(new
                {
                    error = "Vehicle not found.",
                    message = $"No vehicle found for booking ID {createReport.BookingID}."
                });

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                string? imageUrl1 = null, imageUrl2 = null;
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

                var account = new Account(_config["CloudinarySettings:CloudName"], _config["CloudinarySettings:ApiKey"], _config["CloudinarySettings:ApiSecret"]);
                var cloudinary = new Cloudinary(account) { Api = { Secure = true } };


                if (createReport.VehicleImage1 != null)
                {
                    var ext = Path.GetExtension(createReport.VehicleImage1.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext)) return BadRequest("Only JPG, JPEG, PNG allowed.");

                    using var stream1 = createReport.VehicleImage1.OpenReadStream();
                    var uploadResult1 = await cloudinary.UploadAsync(new ImageUploadParams { File = new FileDescription(createReport.VehicleImage1.FileName, stream1), Folder = "car_rental_system/handover_reports" });
                    if (uploadResult1.Error != null) throw new Exception(uploadResult1.Error.Message);
                    imageUrl1 = uploadResult1.SecureUrl.ToString();
                }

                if (createReport.VehicleImage2 != null)
                {
                    var ext = Path.GetExtension(createReport.VehicleImage2.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext)) return BadRequest("Only JPG, JPEG, PNG allowed.");

                    using var stream2 = createReport.VehicleImage2.OpenReadStream();
                    var uploadResult2 = await cloudinary.UploadAsync(new ImageUploadParams { File = new FileDescription(createReport.VehicleImage2.FileName, stream2), Folder = "car_rental_system/handover_reports" });
                    if (uploadResult2.Error != null) throw new Exception(uploadResult2.Error.Message);
                    imageUrl2 = uploadResult2.SecureUrl.ToString();
                }
                var report = new HandoverReport
                {
                    BookingID = createReport.BookingID,
                    ReportType = reportType,
                    StaffID = userID,
                    Mileage = createReport.Mileage,
                    FuelLevel = createReport.FuelLevel,
                    VehicleImage1 = imageUrl1,
                    VehicleImage2 = imageUrl2,
                    Remarks = string.IsNullOrEmpty(createReport.Remarks) ? "No remarks." : createReport.Remarks,
                    HandoverTime = DateTime.Now,
                };
                if (reportType == "Check-Out")
                {
                    booking.Status = "InProgress";
                    vehicle.Status = "Rented";
                }
                else if (reportType == "return")
                {
                    booking.Status = "Completed";
                    vehicle.Status = "Available";
                }
                await _db.HandoverReports.AddAsync(report);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new
                {
                    message = $"{reportType} processed successfully.",
                    ReportID = report.ReportID,
                    BookingStatus = booking.Status,
                    VehicleStatus = vehicle.Status
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    error = "An error occurred while processing the handover report.",
                    message = ex.Message
                });
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }

        // Staff get the list of assigned bookings
        [HttpGet ("assignedbookings")]
        public async Task<IActionResult> GetAssignBooking()
        {
            int staffID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (staffID == 0)
                return NotFound(new
                {
                    error = "User not authenticated.",
                    message = "Please log in to view assigned bookings."
                });
            var bookings = await _db.Bookings
                .Where(b => b.AssignedStaffID == staffID && (b.Status == "InProgress" || b.Status == "Confirmed"))
                .Include(b => b.Vehicle)
                .Include(b => b.User)
                .Include(b => b.DeliveryArea)
                .OrderByDescending(b => b.StartDate)
                .Select(b => new
                {
                    b.BookingID,
                    VehicleModel = b.Vehicle.Model,
                    VehicleLicensePlate = b.Vehicle.LicensePlate,
                    UserName = b.User.UserName,
                    PhoneNumber = b.User.PhoneNumber,
                    b.StartDate,
                    b.EndDate,
                    b.HandoverMethod,
                    DeliveryArea = b.DeliveryArea != null ? b.DeliveryArea.AreaName : "N/A",
                    DeliveryAddress = b.DeliveryAddress != null ? b.DeliveryAddress : "N/A",
                    StaffName = b.AssignedStaff.UserName,
                }).ToListAsync();
            return Ok(bookings);
        }
    }
}
