using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin Operation Management")]
    public class OperationManagementController : Controller
    {
        private readonly AppDbContext _db;
        public OperationManagementController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("unassignedbookings")]
        public async Task<IActionResult> GetUnassignedBookings()
        {
            var bookings = await _db.Bookings
                .Where(b => b.AssignedStaffID == null && b.Status == "Confirmed")
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
                    b.DeliveryAddress
                }).ToListAsync();
            return Ok(bookings);
        }
        [HttpGet("handoverreports")]
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

            return Ok(report);
        }
        [HttpPost("completebooking/{bookingid}")]
        public async Task<IActionResult> CompletedBooking(int bookingid)
        {
            var booking = await _db.Bookings
                .Include(b => b.Vehicle)
                .FirstOrDefaultAsync(b => b.BookingID == bookingid);
            if (booking == null)
                return BadRequest(new
                {
                    error = "Booking not found.",
                    message = $"No booking found with ID {bookingid}."
                });

            if (booking.Status == "Completed")
                return BadRequest(new
                {
                    error = "Booking already completed.",
                    message = $"Booking ID {bookingid} is already marked as completed."
                });

            var reportHandover = await _db.HandoverReports
                .AnyAsync(r => r.BookingID == bookingid && r.ReportType == "return");
            if (!reportHandover)
                return BadRequest(new
                {
                    error = "Return report missing.",
                    message = $"No return handover report found for booking ID {bookingid}. Please submit the return report before marking the booking as completed."
                });

            booking.Status = "Completed";
            if (booking.Vehicle != null)
            {
                booking.Vehicle.Status = "Available";
            }

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = $"Booking ID {bookingid} marked as completed successfully."
            });
        }
    }
}
