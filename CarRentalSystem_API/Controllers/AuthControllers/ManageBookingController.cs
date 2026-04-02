using CarRentalSystem_API.DTO.BookingDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManageBookingController : Controller
    {
        private readonly AppDbContext _db;
        public ManageBookingController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("unavailable-dates/{vehicleid}")]
        public async Task<IActionResult> GetUnavailabelDates(int vehicleid)
        {
            var existingVechile = await _db.Vehicles.AnyAsync(x => x.VehicleID == vehicleid);
            if (!existingVechile)
                return NotFound(new
                {
                    error = "Vehicle not found",
                    Message = $"No vehicle found with ID {vehicleid}"
                });
            var booking = await _db.Bookings
                .Where(x => x.VehicleID == vehicleid &&
                x.EndDate >= DateTime.Today &&
                (x.Status == "Pending" || x.Status == "Confirmed"))
                .ToListAsync();
            var maintenances = await _db.MaintenanceRecords
                .Where(x => x.VehicleID == vehicleid &&
                x.EndDate >= DateTime.Today).ToListAsync();
            var unavailableDates = new List<object>();
            foreach (var b in booking)
            {
                unavailableDates.Add(new
                {
                    StartDate = b.StartDate,
                    EndDate = b.EndDate,
                    Type = "Booking"
                });
            }
            foreach (var m in maintenances)
            {
                unavailableDates.Add(new
                {
                    StartDate = m.StartDate,
                    EndDate = m.EndDate,
                    Type = "Maintenance"
                });
            }
            return Ok(unavailableDates);
        }
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDTO createBooking)
        {
            var existingUser = await _db.Users.AnyAsync(x => x.UserID == createBooking.UserID);
            if (!existingUser)
                return NotFound(new
                {
                    error = "User not found",
                    Message = $"No user found with ID {createBooking.UserID}"
                });

            var existingVechile = await _db.Vehicles.AnyAsync(x => x.VehicleID == createBooking.VehicleID);
            if (!existingVechile)
                return NotFound(new
                {
                    error = "Vehicle not found",
                    Message = $"No vehicle found with ID {createBooking.VehicleID}"
                });
            int totalDays = (createBooking.EndDate - createBooking.StartDate).Days;
            if (totalDays <= 0)
                return BadRequest(new
                {
                    error = "Invalid date range",
                    Message = "End date must be greater than start date"
                });
            else if (totalDays > 30)
                return BadRequest(new
                {
                    error = "Invalid date range",
                    Message = "Booking cannot be longer than 30 days"
                });
            var discountAmount = 0m;
            if (createBooking.PromotionID.HasValue)
            {
                var existingPromotion = await _db.Promotions.FirstOrDefaultAsync(x => x.PromotionID == createBooking.PromotionID.Value && x.StartDate <= DateTime.Today && x.EndDate >= DateTime.Today);
                if (existingPromotion == null)
                    return NotFound(new
                    {
                        error = "Promotion not found",
                        Message = $"No promotion found with ID {createBooking.PromotionID.Value}"
                    });
                discountAmount = (existingPromotion.DiscountPercentage / 100) * createBooking.TotalPrice;
            }
            else
            {
                discountAmount = 0m;
            }
            var finalPaidAmount = createBooking.TotalPrice - discountAmount;
            var booking = new Booking
            {
                UserID = createBooking.UserID,
                VehicleID = createBooking.VehicleID,
                StartDate = createBooking.StartDate,
                EndDate = createBooking.EndDate,
                TotalPrice = createBooking.TotalPrice,
                DiscountAmount = discountAmount,
                FinalPaidAmount = finalPaidAmount,
                PromotionID = createBooking.PromotionID,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = "Booking created successfully",
                BookingID = booking.BookingID,
                Text = $"Please proceed to payment for booking ID {booking.BookingID} with final amount {finalPaidAmount:C}"
            });
        }
        [HttpPost("{bookingid}")]
        public async Task<IActionResult> ProcessPayment(int bookingid)
        {
            var existingBooking = await _db.Bookings.FirstOrDefaultAsync(x => x.BookingID == bookingid);
            if (existingBooking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    Message = $"No booking found with ID {bookingid}"
                });
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                existingBooking.Status = "Confirmed";

                var transactionRecord = new Transaction
                {
                    BookingID = existingBooking.BookingID,
                    Amount = existingBooking.FinalPaidAmount,
                    TransactionDate = DateTime.Now,
                    Type = "Payment",
                    Status = "Completed"
                };
                _db.Transactions.Add(transactionRecord);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new
                {
                    Message = "Payment processed successfully",
                    TransactionID = transactionRecord.TransactionID,
                    BookingID = existingBooking.BookingID,
                    AmountPaid = transactionRecord.Amount,
                    TransactionDate = transactionRecord.TransactionDate
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    error = "Payment processing failed",
                    Message = ex.Message
                });
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
