using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRentalSystem_API.Controllers.UserControllers
{
    [ApiController]
    [Route("api/user/transaction")]
    [Tags("User Transaction Management")]
    [Authorize(Roles = "User")]
    public class TransactionUserController : Controller
    {
       private readonly AppDbContext _db;
        public TransactionUserController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet ("transactions")]
        public async Task<IActionResult> GetTransactionHistory()
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (userID == 0)
            {
                return BadRequest(new
                {
                    error = "Invalid User",
                    message = "User information is missing or invalid. Please log in again to access your transaction history."
                });
            }
            var transaction = await _db.Transactions
                .Include(x => x.Booking)
                .Where(x => x.Booking.UserID == userID)
                .OrderByDescending(x => x.TransactionDate)
                .Select(x => new
                {
                    x.TransactionID,
                    x.TransactionCode,
                    x.Type,
                    x.Amount,
                    x.TransactionDate,
                    x.PaymentMethod,
                    x.Status,
                    RelatedBookingID = x.BookingID
                }).ToListAsync();
            return Ok(transaction);
        }
    }
}
