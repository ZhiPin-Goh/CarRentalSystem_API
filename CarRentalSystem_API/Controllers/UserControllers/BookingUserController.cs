using CarRentalSystem_API.DTO.BookingDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/user/booking")]
    [Tags("User Booking")]
    [Authorize(Roles = "User")]
    public class BookingUserController : Controller
    {
        private readonly AppDbContext _db;
        public BookingUserController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("unavailabledates/{vehicleid}")]
        public async Task<IActionResult> GetUnavailabelDates(int vehicleid)
        {
            var existingVechile = await _db.Vehicles.AnyAsync(x => x.VehicleID == vehicleid);
            if (!existingVechile)
                return NotFound(new
                {
                    error = "Vehicle not found",
                    message = $"No vehicle found with ID {vehicleid}"
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
        [HttpGet ("progress")]
        public async Task<IActionResult> GetBookingProgress()
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var existingUser = await _db.Users.AnyAsync(x => x.UserID == userID);
            if (!existingUser)
                return NotFound(new
                {
                    error = "User not found",
                    message = $"No user found with ID {userID}"
                });
            var bookings = await _db.Bookings
                .Where(x => x.UserID == userID && x.Status == "InProgress" && x.Vehicle.Status == "Rented" && x.StartDate <= DateTime.Today && x.EndDate >= DateTime.Today)
                .Include(x => x.Vehicle)
                .ToListAsync();
            return Ok(bookings);
        }
        [HttpPost ("createbooking")]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDTO createBooking)
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (userID == 0)
                return Unauthorized(new
                {
                    error = "Unauthorized",
                    message = "User must be logged in to create a booking"
                });
            var existingUser = await _db.Users.FirstOrDefaultAsync(x => x.UserID == userID);
            if (existingUser == null)
                return NotFound(new
                {
                    error = "User not found",
                    message = $"No user found with ID {userID}"
                });
            if (existingUser.DriverLicenseNumber == null && existingUser.DriverLicenseImage == null)
                return BadRequest(new
                {
                    error = "Driver's license required",
                    message = "User must have a valid driver's license to create a booking"
                });
            var existingVehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.VehicleID == createBooking.VehicleID);
            if (existingVehicle == null)
                return NotFound(new
                {
                    error = "Vehicle not found",
                    message = $"No vehicle found with ID {createBooking.VehicleID}"
                });

            DateTime startday = createBooking.StartDate.Date;
            DateTime endday = createBooking.EndDate.Date;
            DateTime today = DateTime.Today;

            int totalDays = (endday - startday).Days;
            if (totalDays < 0)
                return BadRequest(new
                {
                    error = "Invalid start date",
                    message = "Start date must be today or in the future"
                });
            else if (totalDays > 30)
                return BadRequest(new
                {
                    error = "Invalid start date",
                    message = "Start date cannot be more than 30 days in the future"
                });

            int rentDurationDays = (startday - today).Days;
            if (rentDurationDays < 0)
                return BadRequest(new
                {
                    error = "Invalid start date",
                    message = "Start date cannot be in the past"
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
                    message = "The selected dates conflict with another booking for the same vehicle"
                });

            decimal deliveryFee = 0;
            string handoverAddress = "";
            if (createBooking.HandoverMethod == "Delivery")
            {
                var deliveryArea = await _db.DeliveryAreas.FirstOrDefaultAsync(x => x.AreaID == createBooking.DeliveryAreaID);
                if (deliveryArea == null || deliveryArea.IsActive == false)
                    return NotFound(new
                    {
                        error = "Delivery area not found",
                        message = $"No delivery area found with ID {createBooking.DeliveryAreaID}"
                    });
                deliveryFee = deliveryArea.Fee;
                handoverAddress = createBooking.DeliveryAddress;
            }
            else if (createBooking.HandoverMethod == "Self-Pickup")
            {
                handoverAddress = $@" No. 104, Ground Floor, Taman City, 
                                      Jalan Kuching, 51200, Kuala Lumpur, 
                                      Wilayah Persekutuan, Malaysia, 51200 Kuala Lumpur";
            }

            decimal carRentalPrice = (existingVehicle.DailyRate * totalDays);
            decimal discountAmount = 0;
            int? appliedPromotionID = null;
            if (!string.IsNullOrEmpty(createBooking.PromotionCode))
            {
                var promotion = await _db.Promotions.FirstOrDefaultAsync(x => x.PromotionCode == createBooking.PromotionCode &&
                x.StartDate <= DateTime.Today &&
                x.EndDate >= DateTime.Today &&
                x.IsActive == true);
                if (promotion == null)
                    return NotFound(new
                    {
                        error = "Promotion not found",
                        message = $"No active promotion found with code {createBooking.PromotionCode}"
                    });
                switch (promotion.PromotionScope)
                {
                    case "Global":

                        break;
                    case "ModelSpecific":
                        if (existingVehicle.Model.ToLower() != promotion.ApplicableModel.ToLower())
                        {
                            return BadRequest(new
                            {
                                error = "Invalid promotion",
                                message = $"Promotion code {createBooking.PromotionCode} is only applicable for {promotion.ApplicableModel} model vehicles"
                            });
                        }
                        break;
                    case "MinSpend":
                        if (carRentalPrice < promotion.TargetValue)
                        {
                            return BadRequest(new
                            {
                                error = "Invalid promotion",
                                message = $"Promotion code {createBooking.PromotionCode} requires a minimum spend of {promotion.TargetValue:C}"
                            });
                        }
                        break;

                    default:
                        return BadRequest(new
                        {
                            error = "Invalid promotion scope",
                            message = $"Promotion code {createBooking.PromotionCode} has an unsupported promotion scope"
                        });
                }
                discountAmount = (carRentalPrice * promotion.DiscountPercentage) / 100;
                appliedPromotionID = promotion.PromotionID;

                decimal theoreticalDiscount = (carRentalPrice * promotion.DiscountPercentage) / 100;
                if (promotion.MaxDiscountAmount.HasValue && theoreticalDiscount > promotion.MaxDiscountAmount.Value)
                {
                    discountAmount = promotion.MaxDiscountAmount.Value;
                }
                else
                {
                    discountAmount = theoreticalDiscount;
                }

                appliedPromotionID = promotion.PromotionID;
            }
            decimal calculatedTotalPrice = carRentalPrice + deliveryFee;
            decimal finalPaidAmount = (carRentalPrice + deliveryFee) - discountAmount;
            var booking = new Booking
            {
                UserID = userID,
                VehicleID = createBooking.VehicleID,
                StartDate = createBooking.StartDate,
                EndDate = createBooking.EndDate,
                TotalPrice = calculatedTotalPrice,
                DiscountAmount = discountAmount,
                FinalPaidAmount = finalPaidAmount,
                PromotionID = appliedPromotionID,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                DeliveryAreaID = createBooking.DeliveryAreaID,
                DeliveryAddress = createBooking.DeliveryAddress,
                HandoverMethod = createBooking.HandoverMethod,
                DeliveryFee = deliveryFee
            };
            await _db.Bookings.AddAsync(booking);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Booking created successfully",
                BookingID = booking.BookingID,
                FinalAmount = finalPaidAmount,
                Text = $"Booking created successfully. Total price: {calculatedTotalPrice:C}, Discount: {discountAmount:C}, Final amount to pay: {finalPaidAmount:C}"
            });
        }
        [HttpPost ("processpayment/{bookingid}")]
        public async Task<IActionResult> ProcessPayment(int bookingid)
        {
            var existingBooking = await _db.Bookings.FirstOrDefaultAsync(x => x.BookingID == bookingid);
            if (existingBooking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    message = $"No booking found with ID {bookingid}"
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
                    message = "Payment processed successfully",
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
                    message = ex.Message
                });
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
        [HttpPost ("extendbooking")]
        public async Task<IActionResult> ExtendBooking([FromBody] ExtendBookingDTO extendBooking)
        {
            var existingBooking = await _db.Bookings.FirstOrDefaultAsync(x => x.BookingID == extendBooking.BookingID);
            if (existingBooking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    message = $"No booking found with ID {extendBooking.BookingID}"
                });
            if (existingBooking.EndDate >= extendBooking.NewDateTime)
                return BadRequest(new
                {
                    error = "Invalid date range",
                    message = "New end date must be greater than current end date"
                });
            int todayDate = (extendBooking.NewDateTime - existingBooking.EndDate).Days;
            if (todayDate > 30)
                return BadRequest(new
                {
                    error = "Invalid date range",
                    message = "Extension cannot be longer than 30 days"
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
                    message = "The new end date conflicts with another booking for the same vehicle"
                });
            return Ok(new
            {
                message = "Booking can be extended",
                BookingID = existingBooking.BookingID,
                CurrentEndDate = existingBooking.EndDate,
                NewEndDate = extendBooking.NewDateTime
            });
        }
        [HttpPost ("processextensionpayment")]
        public async Task<IActionResult> ProcessExtensionPayment([FromBody] ExtendPaymentDTO extendPayment)
        {
            var booking = await _db.Bookings.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.BookingID == extendPayment.BookingID);
            if (booking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    message = $"No booking found with ID {extendPayment.BookingID}"
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
                    message = "Extension payment processed successfully",
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
                    message = ex.Message
                });
            }
            finally
            {
                await dBtransaction.DisposeAsync();
            }
        }
        [HttpPost ("cancelbooking/{bookingid}")]
        public async Task<IActionResult> CancelBooking(int bookingid)
        {
            var existingBooking = await _db.Bookings.FirstOrDefaultAsync(x => x.BookingID == bookingid);
            if (existingBooking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    message = $"No booking found with ID {bookingid}"
                });
            if (existingBooking.Status != "Pending" && existingBooking.Status != "Confirmed")
                return BadRequest(new
                {
                    error = "Invalid booking status",
                    message = "Only bookings with Pending or Confirmed status can be cancelled"
                });

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                decimal refundAmount;
                var timeUntilStart = existingBooking.StartDate - DateTime.Now;
                if (timeUntilStart.TotalHours >= 24)
                    return BadRequest(new
                    {
                        error = "Cancellation not allowed",
                        message = "Bookings can only be cancelled within 24 hours of the start date"
                    });
                else
                {
                    refundAmount = existingBooking.FinalPaidAmount * 0.5m; // 50% refund if cancelled within 24 hours of start date
                }
                var transactionRecord = new Transaction
                {
                    BookingID = existingBooking.BookingID,
                    Amount = refundAmount,
                    TransactionDate = DateTime.Now,
                    Type = "Refund for Booking Cancellation",
                    Status = "Completed"
                };
                _db.Transactions.Add(transactionRecord);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return Ok(new
                {
                    message = "Booking cancelled successfully",
                    BookingID = existingBooking.BookingID,
                    RefundAmount = refundAmount,
                    TransactionID = transactionRecord.TransactionID,
                    TransactionDate = transactionRecord.TransactionDate
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    error = "Booking cancellation failed",
                    message = ex.Message
                });
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
