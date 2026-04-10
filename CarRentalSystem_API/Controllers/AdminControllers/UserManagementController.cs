using CarRentalSystem_API.DTO.UserDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrypt;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [Tags("Admin User Management")]
    public class UserManagementController : Controller
    {
        private readonly AppDbContext _db;
        private static string phonePattern = @"^01[0-9]-\d{7,8}$";
        private static string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$";
        private static string emailPattern = @"^.+@.+$";
        private static ScryptEncoder encoder = new ScryptEncoder();
        public UserManagementController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUser()
        {
            var users = await _db.Users
                 .Where(u => u.Role == "User")
                 .Select(u => new
                 {
                     u.UserID,
                     u.UserName,
                     u.Email,
                     u.PhoneNumber,
                     u.Status,
                     u.DriverLicenseImage,
                     u.DriverLicenseNumber
                 })
                 .ToListAsync();
            return Ok(users);
        }
        [HttpPost ("createuser")]
        public async Task<IActionResult> CreateUserAdmin([FromBody] CreateUserDTO createUser)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.PhoneNumber.ToString(), phonePattern))
            {
                return BadRequest(new
                {
                    error = "Invalid Phone Number Format",
                    message = "Invalid phone number format. Format: 01X-XXXXXXX"
                });
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.Password, passwordPattern))
            {
                return BadRequest(new
                {
                    error = "Invalid Password Format",
                    message = "Password must be at least 8 characters long and include uppercase, lowercase, digit, and special character."
                });
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.Email, emailPattern))
            {
                return BadRequest(new
                {
                    error = "Invalid Email Format",
                    message = "Invalid email format."
                });
            }
            var existingUser = await _db.Users.AnyAsync(u => u.Email == createUser.Email && u.PhoneNumber == createUser.PhoneNumber);
            if (existingUser)
            {
                return BadRequest(new
                {
                    error = "User Already Exists",
                    message = "A user with the same email and phone number already exists."
                });
            }
            await _db.Users.AddAsync(new User
            {
                UserName = createUser.UserName,
                Email = createUser.Email,
                PhoneNumber = createUser.PhoneNumber,
                Password = encoder.Encode(createUser.Password),
                Status = "Active",
                Role = "User"
            });
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "User created successfully.",
                UserID = _db.Users.OrderByDescending(u => u.UserID).FirstOrDefault().UserID
            });
        }
        [HttpPost("toggleuserstatus/{id}")]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.UserID == id);
            if (existingUser == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = $"User with ID {id} not found."
                });
            }
            existingUser.Status = existingUser.Status == "Active" ? "Inactive" : "Active";
            _db.Entry(existingUser).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = $"User status changed to {existingUser.Status} successfully.",
                UserID = existingUser.UserID,
                NewStatus = existingUser.Status
            });
        }
    }
}
