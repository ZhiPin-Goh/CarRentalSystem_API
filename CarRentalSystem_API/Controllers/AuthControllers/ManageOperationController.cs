using CarRentalSystem_API.DTO.HandoverReportDTO;
using CarRentalSystem_API.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManageOperationController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public ManageOperationController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        /// <summary>
        /// This is Handover Report API
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetHandoverReport()
        {
            var report = await _db.HandoverReports
                .Select(r => new
                {
                    r.ReportID,
                    r.BookingID,
                    r.ReportType,
                    r.StaffID,
                    StaffName = r.Staff.UserName,
                    r.Mileage,
                    r.FuelLevel,
                    r.VehicleImage1,
                    r.VehicleImage2,
                    r.Remarks,
                    r.HandoverTime
                })
                .ToListAsync();
            if (report == null)
            {
                return NotFound(new
                {
                    error = "No handover reports found.",
                    Message = "No Reports is found in the system."
                });
            }
            return Ok(report);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitCheckOut([FromForm] CreateReportDTO createReport)
        {
            return await ProcessHandover(createReport, "Check-Out");
        }
        [HttpPost]
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
                    Message = "Please ensure all required fields are filled correctly."
                });
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userID == 0)
                return Unauthorized(new
                {
                    error = "User not authenticated.",
                    Message = "Please log in to submit a handover report."
                });

            var booking = await _db.Bookings.FindAsync(createReport.BookingID);
            if (booking == null)
                return NotFound(new
                {
                    error = "Booking not found.",
                    Message = $"No booking found with ID {createReport.BookingID}."
                });
            var vehicle = await _db.Vehicles.FindAsync(booking.VehicleID);
            if (vehicle == null)
                return NotFound(new
                {
                    error = "Vehicle not found.",
                    Message = $"No vehicle found for booking ID {createReport.BookingID}."
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
                    Message = $"{reportType} processed successfully.",
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
                    Message = ex.Message
                });
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
        // Handover Report API finished

        ///<summary>
        /// This is booking operation API.
        ///</summary>

        [HttpPost]
        public async Task<IActionResult> AssignStaff([FromBody] AssignStaffDTO assign)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.BookingID == assign.BookingID && b.HandoverMethod == "Self-Service");
            if (booking == null)
                return NotFound(new
                {
                    error = "Booking not found.",
                    Message = $"No booking found with ID {assign.BookingID}."
                });
            var staff = await _db.Users.FindAsync(assign.StaffID);
            if (staff == null || staff.Role != "Staff")
                return NotFound(new
                {
                    error = "Staff not found.",
                    Message = $"No staff found with ID {assign.StaffID}."
                });
            booking.AssignedStaffID = assign.StaffID;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = $"Staff {staff.UserName} assigned to booking ID {assign.BookingID} successfully."
            });
        }
        [HttpGet]
        public async Task<IActionResult> GetAssignBooking()
        {
            int staffID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (staffID == 0)
                return NotFound(new
                {
                    error = "User not authenticated.",
                    Message = "Please log in to view assigned bookings."
                });
            var bookings = await _db.Bookings
                .Select(x => new
                {
                    x.BookingID,
                    x.VehicleID,
                    VehicleModel = x.Vehicle.Model,
                    x.User.UserName,
                    x.DeliveryArea,
                    x.DeliveryAddress,
                    x.HandoverMethod,
                    x.StartDate,
                    x.Status,
                    x.AssignedStaffID
                }).ToListAsync();
            return Ok(bookings);
        }
    }
}
