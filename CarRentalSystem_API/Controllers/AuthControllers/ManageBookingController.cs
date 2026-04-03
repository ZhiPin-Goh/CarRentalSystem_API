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
            var existingVehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.VehicleID == createBooking.VehicleID);
            if (existingVehicle == null)
                return NotFound(new
                {
                    error = "Vehicle not found",
                    Message = $"No vehicle found with ID {createBooking.VehicleID}"
                });
            int totalDays = (createBooking.StartDate - DateTime.Today).Days;
            if (totalDays > 0) 
                return BadRequest(new
                {
                    error = "Invalid start date",
                    Message = "Start date must be today or in the future"
                });
            else if (totalDays > 30)
                return BadRequest(new
                {
                    error = "Invalid start date",
                    Message = "Start date cannot be more than 30 days in the future"
                });
            bool isConflict = await _db.Bookings.AnyAsync(x => 
            x.VehicleID == createBooking.VehicleID &&
            (x.Status == "Pending" || x.Status == "Confirmed" || x.Status == "InProgress") &&
            x.StartDate < createBooking.EndDate &&
            x.EndDate > createBooking.StartDate);
            if (isConflict)
                return BadRequest(new
                {
                    error = "Date conflict",
                    Message = "The selected dates conflict with another booking for the same vehicle"
                });

            decimal calculatedTotalPrice = existingVehicle.DailyRate * totalDays;
            decimal discountAmount = 0;
            int? appliedPromotionID = null;
            if (!string.IsNullOrEmpty(createBooking.PromotionCode))
            {
                var promotion = await _db.Promotions.FirstOrDefaultAsync( x=> x.PromotionCode == createBooking.PromotionCode &&
                x.StartDate <= DateTime.Today &&
                x.EndDate >= DateTime.Today &&
                x.IsActive == true);
                if (promotion != null)
                    return NotFound(new
                    {
                        error = "Promotion not found",
                        Message = $"No active promotion found with code {createBooking.PromotionCode}"
                    });
                if (promotion.PromotionScope == "Type" && !string.Equals(existingVehicle.Type, promotion.TargetValue, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        error = "Invalid Promotion",
                        Message = "This promotion code is only applicable for " + promotion.TargetValue + " type vehicles!"

                    });
                }
                else if (promotion.PromotionScope == "Model" && !string.Equals(existingVehicle.Model, promotion.TargetValue, StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new
                    {
                        error = "Invalid Promotion",
                        Message = "This promotion code is only applicable for " + promotion.TargetValue + " model vehicles!"
                    });
                }
                discountAmount = (calculatedTotalPrice * promotion.DiscountPercentage) / 100;
                appliedPromotionID = promotion.PromotionID;
            }
            var finalPaidAmount = calculatedTotalPrice - discountAmount;
            var booking = new Booking
            {
                UserID = createBooking.UserID,
                VehicleID = createBooking.VehicleID,
                StartDate = createBooking.StartDate,
                EndDate = createBooking.EndDate,
                TotalPrice = calculatedTotalPrice,
                DiscountAmount = discountAmount,
                FinalPaidAmount = finalPaidAmount,
                PromotionID = appliedPromotionID,
                Status = "Pending"
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = "Booking created successfully",
                BookingID = booking.BookingID,
                FinalAmount = finalPaidAmount,
                Text = $"Booking created successfully. Total price: {calculatedTotalPrice:C}, Discount: {discountAmount:C}, Final amount to pay: {finalPaidAmount:C}"
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
                    Type = "Payment New Booking",
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
        [HttpPost]
        public async Task<IActionResult> ExtendBooking([FromBody] ExtendBookingDTO extendBooking)
        {
            var existingBooking = await _db.Bookings.FirstOrDefaultAsync(x => x.BookingID == extendBooking.BookingID);
            if (existingBooking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    Message = $"No booking found with ID {extendBooking.BookingID}"
                });
            if (existingBooking.EndDate >= extendBooking.NewDateTime)
                return BadRequest(new
                {
                    error = "Invalid date range",
                    Message = "New end date must be greater than current end date"
                });
            int todayDate = (extendBooking.NewDateTime = existingBooking.StartDate).Day;
            if (todayDate > 30)
                return BadRequest(new
                {
                    error = "Invalid date range",
                    Message = "Extension cannot be longer than 30 days"
                });

            bool isConflicting = await _db.Bookings
                .AnyAsync(x => x.VehicleID == existingBooking.VehicleID &&
                x.BookingID != existingBooking.BookingID &&
                (x.Status == "Pending" || x.Status == "Confirmed" || x.Status == "InProgress") &&
                x.StartDate < extendBooking.NewDateTime &&
                x.EndDate > existingBooking.EndDate);
            if (isConflicting)
                return BadRequest(new
                {
                    error = "Date conflict",
                    Message = "The new end date conflicts with another booking for the same vehicle"
                });
            return Ok(new
            {
                Message = "Booking can be extended",
                BookingID = existingBooking.BookingID,
                CurrentEndDate = existingBooking.EndDate,
                NewEndDate = extendBooking.NewDateTime
            });
        }
        [HttpPost]
        public async Task<IActionResult> ProcessExtensionPayment([FromBody] ExtendPaymentDTO extendPayment)
        {
            var booking = await _db.Bookings.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.BookingID == extendPayment.BookingID);
            if (booking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    Message = $"No booking found with ID {extendPayment.BookingID}"
                });
            int extraDays = (extendPayment.NewEndDate - booking.EndDate).Days;
            decimal extraCost = extraDays * booking.Vehicle.DailyRate;
            using var dBtransaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var transaction = new Transaction
                {
                    BookingID = booking.BookingID,
                    Amount = extraCost,
                    TransactionDate = DateTime.Now,
                    Type = "ExtensionPayment",
                    Status = "Success",
                };
                _db.Transactions.Add(transaction);
                booking.EndDate = extendPayment.NewEndDate;
                booking.TotalPrice += extraCost;
                booking.FinalPaidAmount += extraCost;
                booking.IsExtended = true;
                await _db.SaveChangesAsync();
                await dBtransaction.CommitAsync();
                return Ok(new
                {
                    Message = "Extension payment processed successfully",
                    BookingID = booking.BookingID,
                    NewEndDate = booking.EndDate,
                    ExtraDays = extraDays,
                    ExtraCost = extraCost,
                    TotalPrice = booking.TotalPrice,
                    FinalPaidAmount = booking.FinalPaidAmount
                });
            }
            catch (Exception ex)
            {
                await dBtransaction.RollbackAsync();
                return StatusCode(500, new
                {
                    error = "Extension payment processing failed",
                    Message = ex.Message
                });
            }
            finally
            {
                await dBtransaction.DisposeAsync();
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetAllBooking()
        {
            var bookings = await _db.Bookings
                .Include(x => x.User)
                .Include(x => x.Vehicle)
                .Include(x => x.Promotion)
                .ToListAsync();
            var bookingList = bookings.Select(b => new
            {
                BookingID = b.BookingID,
                UserName = b.User,
                VehicleModel = b.Vehicle.Model,
                StartDate = b.StartDate,
                EndDate = b.EndDate,
                TotalPrice = b.TotalPrice,
                DiscountAmount = b.DiscountAmount,
                FinalPaidAmount = b.FinalPaidAmount,
                PromotionCode = b.Promotion != null ? b.Promotion.PromotionCode : null,
                Status = b.Status
            });
            return Ok(bookingList);
        }
        [HttpGet("{userid}")]
        public async Task<IActionResult> GetBookingProgress(int userid)
        {
            var existingUser = await _db.Users.AnyAsync(x => x.UserID == userid);
            if (!existingUser)
                return NotFound(new
                {
                    error = "User not found",
                    Message = $"No user found with ID {userid}"
                });
            var bookings = await _db.Bookings
                .Where(x => x.UserID == userid && x.Status == "InProgress" && x.Vehicle.Status == "Rented" && x.StartDate <= DateTime.Today && x.EndDate >= DateTime.Today)
                .Include(x => x.Vehicle)
                .ToListAsync();
            return Ok(bookings);
        }
    }
}
