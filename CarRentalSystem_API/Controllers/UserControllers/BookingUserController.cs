using CarRentalSystem_API.DTO.BookingDTO;
using CarRentalSystem_API.Function;
using CarRentalSystem_API.Interface;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/user/booking")]
    [Tags("User Booking")]
    [Authorize(Roles = "User")]
    public class BookingUserController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ILogger<BookingUserController> _logger;
        private readonly IPdfService _pdfService;
        public BookingUserController(AppDbContext db, ILogger<BookingUserController> logger, IPdfService pdfService)
        {
            _db = db;
            _logger = logger;
            _pdfService = pdfService;
        }
        [HttpGet("unavailabledates/{vehicleid}")]
        public async Task<IActionResult> GetUnavailableDates(int vehicleid)
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
        [HttpGet("unavailableextenddates")]
        public async Task<IActionResult> GetUnavailableExtendDates([FromQuery] UnavailableExtendDatesDTO request)
        {
            var query = _db.Bookings.Where(x => x.VehicleID == request.VehicleID &&
                    (x.Status == "Pending" || x.Status == "Confirmed" || x.Status == "InProgress") &&
                    x.EndDate >= DateTime.Today);

            if (request.ExcludeBookingID.HasValue)
            {
                query = query.Where(x => x.BookingID != request.ExcludeBookingID.Value);
            }
            var unavailablePeriods = await query
                .Select(x => new
                {
                    Start = x.StartDate.ToString("yyyy-MM-dd"),
                    End = x.EndDate.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(unavailablePeriods);
        }
        [HttpPost("bookingprice")]
        public async Task<IActionResult> GetBookingPrice([FromBody] BookingPriceRequestDTO request)
        {
            var existingVehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.VehicleID == request.VehicleID);
            if (existingVehicle == null)
            {
                return NotFound(new
                {
                    error = "Vehicle not found",
                    message = $"No vehicle found with ID {request.VehicleID}"
                });
            }
            if (existingVehicle.Status != "Available")
            {
                return BadRequest(new
                {
                    error = "Vehicle not available",
                    message = $"The vehicle with ID {request.VehicleID} is currently not available for booking"
                });
            }
            int totalDays = (request.EndDate.Date - request.StartDate.Date).Days;
            if (totalDays <= 0)
            {
                return NotFound(new
                {
                    error = "Invalid date range",
                    message = "End date must be greater than start date"
                });
            }
            decimal deliveryPrice = 0;
            if (request.DeliveryAreaID.HasValue)
            {
                var existingDeliveryArea = await _db.DeliveryAreas.FirstOrDefaultAsync(x => x.AreaID == request.DeliveryAreaID.Value && x.IsActive == true);
                if (existingDeliveryArea == null)
                {
                    return NotFound(new
                    {
                        error = "Delivery area not found",
                        message = $"No active delivery area found with ID {request.DeliveryAreaID.Value}"
                    });
                }
                deliveryPrice = existingDeliveryArea.Fee;
            }

            decimal carRentalPrice = existingVehicle.DailyRate * totalDays;
            decimal discountAmount = 0;
            int? appliedPromotionID = null;
            if (!string.IsNullOrEmpty(request.PromoCode))
            {
                var promotion = await _db.Promotions.FirstOrDefaultAsync(x => x.PromotionCode == request.PromoCode &&
                x.StartDate <= DateTime.Today &&
                x.EndDate >= DateTime.Today &&
                x.IsActive == true);
                if (promotion == null)
                    return NotFound(new
                    {
                        error = "Promotion not found",
                        message = $"No active promotion found with code {request.PromoCode}"
                    });
                switch (promotion.PromotionScope)
                {
                    case "Global":
                        break;
                    case "ModelSpecific":
                        if (existingVehicle.Model.ToLower() != promotion.ApplicableModel.ToLower())
                        {
                            return BadRequest(new { error = "Invalid promotion", message = $"Promotion code {request.PromoCode} is only applicable for {promotion.ApplicableModel} model vehicles" });
                        }
                        break;
                    case "MinSpend":
                        if (carRentalPrice < promotion.TargetValue)
                        {
                            return BadRequest(new { error = "Invalid promotion", message = $"Promotion code {request.PromoCode} requires a minimum spend of RM {promotion.TargetValue:F2}" });
                        }
                        break;
                    default:
                        return BadRequest(new { error = "Invalid promotion scope", message = $"Promotion code {request.PromoCode} has an unsupported promotion scope" });
                }
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

            decimal calculatedTotalPrice = carRentalPrice + deliveryPrice;
            decimal finalPrice = calculatedTotalPrice - discountAmount;
            if (finalPrice < 0) finalPrice = 0;
            return Ok(new
            {
                VehicleID = existingVehicle.VehicleID,
                VehicleName = $"{existingVehicle.Brand} {existingVehicle.Model}",
                TotalDays = totalDays,
                CarRentalPrice = carRentalPrice,
                DeliveryPrice = deliveryPrice,
                DiscountAmount = discountAmount,
                FinalPrice = finalPrice,
                AppliedPromotionID = appliedPromotionID
            });

        }
        [HttpGet("progress")]
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
                .Include(x => x.Vehicle)
                .Include(x => x.Vehicle.VehicleImages.Where(img => img.IsPrimary))
                .Where(x => x.UserID == userID && (x.Status == "Pending" || x.Status == "Confirmed" || x.Status == "InProgress"))
                .Include(x => x.Vehicle)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new
                {
                    BookingID = x.BookingID,
                    VehicleName = x.Vehicle.Model,
                    VehicleBrand = x.Vehicle.Brand,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate,
                    TotalPrice = x.TotalPrice,
                    Status = x.Status,
                    IsActiveToday = (x.StartDate.Date <= DateTime.Today && x.EndDate.Date >= DateTime.Today),
                    PrimaryImageURL = x.Vehicle.VehicleImages.FirstOrDefault(img => img.IsPrimary).ImageURL
                })
                .ToListAsync();
            return Ok(bookings);
        }
        [HttpGet("pending/{bookingID}")]
        public async Task<IActionResult> GetPendingPayment(int bookingID)
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var existingUser = await _db.Users.AnyAsync(x => x.UserID == userID);
            if (!existingUser)
                return NotFound(new
                {
                    error = "User not found",
                    message = $"No user found with ID {userID}"
                });
            var booking = await _db.Bookings
                .Include(x => x.Vehicle)
                .FirstOrDefaultAsync(x => x.UserID == userID && x.BookingID == bookingID && x.Status == "Pending");
            if (booking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    message = $"No pending booking found with ID {bookingID} for the user"
                });
            var pendingObj = new
            {
                BookingID = booking.BookingID,
                Brand = booking.Vehicle.Brand,
                Model = booking.Vehicle.Model,
                StartDate = booking.StartDate,
                EndDate = booking.EndDate,
                TotalAmount = booking.FinalPaidAmount
            };
            return Ok(pendingObj);
        }
        [HttpPost("createbooking")]
        [EnableRateLimiting("StrictPolicy")]
        public async Task<IActionResult> CreateBooking(
            [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
            [FromBody] CreateBookingDTO createBooking)
        {
            // Idempotency check
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest(new
                {
                    error = "Idempotency key required",
                    message = "Please provide a unique idempotency key in the X-Idempotency-Key header to prevent duplicate bookings"
                });
            var existingIdempotency = await _db.Idempotencies.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
            if (existingIdempotency != null)
            {
                if (existingIdempotency.Status == "Completed")
                {
                    return Content(existingIdempotency.ResponseBody, "application/json");
                }
                if (existingIdempotency.Status == "Started")
                {
                    return Conflict(new { error = "Duplicate Request", message = "This booking is currently being processed. Please wait." });
                }
            }
            var currentIdempotency = new Idempotency
            {
                IdempotencyKey = idempotencyKey,
                RequestHash = "BookingRequest",
                Status = "Started",
                CreatedAt = DateTime.Now
            };
            await _db.Idempotencies.AddAsync(currentIdempotency);
            await _db.SaveChangesAsync();
            try
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
                var responseObj = new
                {
                    message = "Booking created successfully",
                    BookingID = booking.BookingID,
                    FinalAmount = finalPaidAmount,
                    Text = $"Booking created successfully. Total price: {calculatedTotalPrice:C}, Discount: {discountAmount:C}, Final amount to pay: {finalPaidAmount:C}"

                };
                currentIdempotency.Status = "Completed";
                currentIdempotency.ResponseCode = 200;
                currentIdempotency.ResponseBody = JsonSerializer.Serialize(responseObj);
                await _db.SaveChangesAsync();
                return Ok(responseObj);
            }
            catch (Exception ex)
            {
                currentIdempotency.Status = "Failed";
                currentIdempotency.ResponseCode = 500;
                currentIdempotency.ResponseBody = ex.Message.ToString();
                await _db.SaveChangesAsync();
                return StatusCode(500, new
                {
                    error = "Booking creation failed",
                    message = ex.Message
                });
            }
        }
        [HttpPost("processpayment/{bookingid}")]
        [EnableRateLimiting("StrictPolicy")]
        public async Task<IActionResult> ProcessPayment(
            [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
            int bookingid)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest(new
                {
                    error = "Idempotency key required",
                    message = "Please provide a unique idempotency key in the X-Idempotency-Key header to prevent duplicate payments"
                });
            var existingIdempotency = await _db.Idempotencies.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
            if (existingIdempotency != null)
            {
                if (existingIdempotency.Status == "Completed")
                {
                    return Content(existingIdempotency.ResponseBody, "application/json");
                }
                if (existingIdempotency.Status == "Started")
                {
                    return Conflict(new
                    {
                        error = "Duplicate Request",
                        message = "This payment is currently being processed. Please wait."
                    });
                }
            }
            var currentIdempotency = new Idempotency
            {
                IdempotencyKey = idempotencyKey,
                RequestHash = $"ProcessPayment_{bookingid}",
                Status = "Started",
                CreatedAt = DateTime.Now
            };
            await _db.Idempotencies.AddAsync(currentIdempotency);
            await _db.SaveChangesAsync();
            var existingBooking = await _db.Bookings
                .Include(x => x.Vehicle)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.BookingID == bookingid);
            if (existingBooking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    message = $"No booking found with ID {bookingid}"
                });
            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                int totalDays = (existingBooking.EndDate - existingBooking.StartDate).Days;
                existingBooking.Status = "Confirmed";

                var transactionRecord = new Transaction
                {
                    BookingID = existingBooking.BookingID,
                    Amount = existingBooking.FinalPaidAmount,
                    TransactionDate = DateTime.Now,
                    Type = "Payment New Booking",
                    PaymentMethod = "Credit Card",
                    TransactionCode = GeneralServices.GenerateNumber(10),
                    Status = "Completed"
                };
                _db.Transactions.Add(transactionRecord);
                var responseObj = new
                {
                    message = "Payment processed successfully",
                    TransactionID = transactionRecord.TransactionID,
                    BookingID = existingBooking.BookingID,
                    AmountPaid = transactionRecord.Amount,
                    TransactionDate = transactionRecord.TransactionDate
                };
                currentIdempotency.Status = "Completed";
                currentIdempotency.ResponseCode = 200;
                currentIdempotency.ResponseBody = JsonSerializer.Serialize(responseObj);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                try
                {
                    await _pdfService.InvoicePdfAsync(
                        userName: existingBooking.User.UserName,
                        email: existingBooking.User.Email,
                        vehicleAmount: existingBooking.FinalPaidAmount,
                        transactionCode: transactionRecord.TransactionCode,
                        totalDate: totalDays,
                        vehicleName: $"{existingBooking.Vehicle.Brand} {existingBooking.Vehicle.Model}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate or send invoice PDF for TransactionID {TransactionID}", transactionRecord.TransactionID);
                }
                return Ok(responseObj);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                currentIdempotency.Status = "Failed";
                currentIdempotency.ResponseCode = 500;
                currentIdempotency.ResponseBody = ex.Message.ToString();
                await _db.SaveChangesAsync();
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
        [HttpPost("extendbooking")]
        public async Task<IActionResult> ExtendBooking([FromBody] ExtendBookingDTO extendBooking)
        {
            var existingBooking = await _db.Bookings.Include(x => x.Vehicle).Include(x => x.User).FirstOrDefaultAsync(x => x.BookingID == extendBooking.BookingID);
            if (existingBooking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    message = $"No booking found with ID {extendBooking.BookingID}"
                });
            if (existingBooking.EndDate.Date >= extendBooking.NewDateTime.Date)
                return BadRequest(new
                {
                    error = "Invalid date range",
                    message = "New end date must be greater than current end date"
                });
            int extraDays = (extendBooking.NewDateTime.Date - existingBooking.EndDate.Date).Days;
            if (extraDays > 30)
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
            decimal extraCost = extraDays * existingBooking.Vehicle.DailyRate;

            return Ok(new
            {
                message = "Booking can be extended",
                BookingID = existingBooking.BookingID,
                CurrentEndDate = existingBooking.EndDate,
                NewEndDate = extendBooking.NewDateTime,
                ExtraDays = extraDays,
                ExtraCost = extraCost,
            });
        }
        [HttpPost("processextensionpayment")]
        [EnableRateLimiting("StrictPolicy")]
        public async Task<IActionResult> ProcessExtensionPayment(
            [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
            [FromBody] ExtendPaymentDTO extendPayment)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest(new
                {
                    error = "Idempotency key required",
                    message = "Please provide a unique idempotency key in the X-Idempotency-Key header to prevent duplicate payments"
                });
            var existingIdempotency = await _db.Idempotencies.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
            if (existingIdempotency != null)
            {
                if (existingIdempotency.Status == "Completed")
                {
                    return Content(existingIdempotency.ResponseBody, "application/json");
                }
                if (existingIdempotency.Status == "Started")
                {
                    return Conflict(new
                    {
                        error = "Duplicate Request",
                        message = "This extension payment is currently being processed. Please wait."
                    });
                }
            }
            var currentIdempotency = new Idempotency
            {
                IdempotencyKey = idempotencyKey,
                RequestHash = $"ProcessExtensionPayment_{extendPayment.BookingID}",
                Status = "Started",
                CreatedAt = DateTime.Now
            };
            await _db.Idempotencies.AddAsync(currentIdempotency);
            await _db.SaveChangesAsync();

            var booking = await _db.Bookings.Include(x => x.Vehicle).Include(x => x.User).FirstOrDefaultAsync(x => x.BookingID == extendPayment.BookingID);
            if (booking == null)
                return NotFound(new
                {
                    error = "Booking not found",
                    message = $"No booking found with ID {extendPayment.BookingID}"
                });
            int extraDays = (extendPayment.NewEndDate - booking.EndDate).Days;
            decimal extraCost = extraDays * booking.Vehicle.DailyRate;

            DateTime oldEndDate = booking.EndDate;
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
                    TransactionCode = GeneralServices.GenerateNumber(10),
                };
                _db.Transactions.Add(transaction);
                booking.EndDate = extendPayment.NewEndDate;
                booking.TotalPrice += extraCost;
                booking.FinalPaidAmount += extraCost;
                booking.IsExtended = true;

                var responseObj = new
                {
                    message = "Extension payment processed successfully",
                    BookingID = booking.BookingID,
                    NewEndDate = booking.EndDate,
                    ExtraDays = extraDays,
                    ExtraCost = extraCost,
                    TotalPrice = booking.TotalPrice,
                    FinalPaidAmount = booking.FinalPaidAmount
                };
                currentIdempotency.Status = "Completed";
                currentIdempotency.ResponseCode = 200;
                currentIdempotency.ResponseBody = JsonSerializer.Serialize(responseObj);
                await _db.SaveChangesAsync();
                await dBtransaction.CommitAsync();


                //use Task.Run to generate and send the invoice email in the background without blocking the main thread
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _pdfService.ExtensionInvoicePdfAsync(
                            userName: booking.User.UserName,
                            email: booking.User.Email,
                            transactionCode: transaction.TransactionCode,
                            vehicleName: booking.Vehicle.Model,
                            dailyRate: booking.Vehicle.DailyRate,
                            oldEndDate: oldEndDate,
                            newEndDate: extendPayment.NewEndDate);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate or send extension invoice PDF for TransactionID {TransactionID}", transaction.TransactionID);
                    }
                });
                return Ok(responseObj);
            }
            catch (Exception ex)
            {
                await dBtransaction.RollbackAsync();
                currentIdempotency.Status = "Failed";
                currentIdempotency.ResponseCode = 500;
                currentIdempotency.ResponseBody = ex.Message.ToString();
                await _db.SaveChangesAsync();
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
        [HttpPost("cancelbooking/{bookingid}")]
        [EnableRateLimiting("StrictPolicy")]
        public async Task<IActionResult> CancelBooking(
            [FromHeader(Name = "X-Idempotency-Key")] string idempotencyKey,
            int bookingid)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                return BadRequest(new
                {
                    error = "Idempotency key required",
                    message = "Please provide a unique idempotency key in the X-Idempotency-Key header to prevent duplicate cancellations"
                });
            var existingIdempotency = await _db.Idempotencies.FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey);
            if (existingIdempotency != null)
            {
                if (existingIdempotency.Status == "Completed")
                {
                    return Content(existingIdempotency.ResponseBody, "application/json");
                }
                if (existingIdempotency.Status == "Started")
                {
                    return Conflict(new
                    {
                        error = "Duplicate Request",
                        message = "This cancellation is currently being processed. Please wait."
                    });
                }
            }
            var currentIdempotency = new Idempotency
            {
                IdempotencyKey = idempotencyKey,
                RequestHash = $"CancelBooking_{bookingid}",
                Status = "Started",
                CreatedAt = DateTime.Now
            };
            await _db.Idempotencies.AddAsync(currentIdempotency);
            await _db.SaveChangesAsync();

            var existingBooking = await _db.Bookings
                .Include(x => x.User)
                .Include(x => x.Vehicle)
                .FirstOrDefaultAsync(x => x.BookingID == bookingid);

            if (existingBooking == null)
                return NotFound(new { error = "Booking not found", message = $"No booking found with ID {bookingid}" });

            if (existingBooking.Status != "Pending" && existingBooking.Status != "Confirmed")
                return BadRequest(new { error = "Invalid status", message = "Only Pending or Confirmed bookings can be cancelled." });

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var timeUntilStart = existingBooking.StartDate - DateTime.Now;

                if (timeUntilStart.TotalHours < 0)
                    return BadRequest(new { error = "Too late", message = "Cannot cancel a booking that has already started." });

                decimal refundAmount = 0;
                Transaction? transactionRecord = null;

                if (existingBooking.Status == "Confirmed")
                {
                    if (timeUntilStart.TotalHours >= 24)
                    {
                        refundAmount = existingBooking.FinalPaidAmount;
                    }
                    else
                    {
                        refundAmount = existingBooking.FinalPaidAmount * 0.5m;
                    }

                    // 生成退款记录
                    transactionRecord = new Transaction
                    {
                        BookingID = existingBooking.BookingID,
                        Amount = refundAmount,
                        TransactionDate = DateTime.Now,
                        Type = "Refund for Booking Cancellation",
                        Status = "Completed",
                        TransactionCode = GeneralServices.GenerateNumber(10)
                    };
                    _db.Transactions.Add(transactionRecord);
                }

                existingBooking.Status = "Cancelled";
                
                var responseObj = new
                {
                    message = "Booking cancelled successfully",
                    BookingID = existingBooking.BookingID,
                    RefundAmount = refundAmount,
                    HasRefund = refundAmount > 0
                };
                currentIdempotency.Status = "Completed";
                currentIdempotency.ResponseCode = 200;
                currentIdempotency.ResponseBody = JsonSerializer.Serialize(responseObj);

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                if (transactionRecord != null)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _pdfService.CancelReceiptPdfAsync(
                                userName: existingBooking.User.UserName,
                                email: existingBooking.User.Email,
                                transactionCode: transactionRecord.TransactionCode,
                                vehicleName: $"{existingBooking.Vehicle.Brand} {existingBooking.Vehicle.Model}",
                                originalAmount: existingBooking.FinalPaidAmount,
                                refundAmount: refundAmount
                        );

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to generate or send cancellation receipt PDF for TransactionID {TransactionID}", transactionRecord.TransactionID);
                        }

                    });
                }

                return Ok(responseObj);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                currentIdempotency.Status = "Failed";
                currentIdempotency.ResponseCode = 500;
                currentIdempotency.ResponseBody = ex.Message.ToString();
                await _db.SaveChangesAsync();
                return StatusCode(500, new { error = "Cancellation failed", message = ex.Message });
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
