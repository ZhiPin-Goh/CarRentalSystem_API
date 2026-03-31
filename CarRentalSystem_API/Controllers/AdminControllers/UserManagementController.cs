using CarRentalSystem_API.DTO.UserDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrypt;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
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
        [HttpGet]
        public async Task<IActionResult> GetAllUser()
        {
            return Ok(await _db.Users.ToListAsync());
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserByID(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserID == id);
            if (user == null)
                return NotFound();

            return Ok(user);
        }
        [HttpPost]
        public async Task<IActionResult> CreateUserAdmin([FromBody] CreateUserDTO createUser)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.PhoneNumber.ToString(), phonePattern))
                return BadRequest("Invalid phone number format. Format: 01X-XXXXXXX");
            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.Password, passwordPattern))
                return BadRequest("Password must be at least 8 characters long and include uppercase, lowercase, digit, and special character.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.Email, emailPattern))
                return BadRequest("Invalid email format.");
            var existingUser = await _db.Users.AnyAsync(u => u.Email == createUser.Email && u.PhoneNumber == createUser.PhoneNumber);
            if (existingUser)
                return BadRequest("A user with the same email and phone number already exists.");
            _db.Users.Add(new User
            {
                UserName = createUser.UserName,
                Email = createUser.Email,
                PhoneNumber = createUser.PhoneNumber,
                Password = encoder.Encode(createUser.Password),
                Status = "Active",
                DriverLicenseNumber = null,
                TelegramID = null
            });
            await _db.SaveChangesAsync();
            return Ok("User created successfully.");
        }
        [HttpPost("{id}")]
        public async Task<IActionResult> ActiveUser([FromBody] int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserID == id);
            if (user == null)
                return NotFound("User not found.");
            user.Status = "Active";
            _db.Entry(user).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return Ok("User activated successfully.");
        }
        [HttpPost("{id}")]
        public async Task<IActionResult> InactiveUser([FromBody] int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserID == id);
            if (user == null)
                return NotFound("User not found.");
            user.Status = "Inactive";
            _db.Entry(user).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return Ok("User inactivated successfully.");
        }
    }
}
