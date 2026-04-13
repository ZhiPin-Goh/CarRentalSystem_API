using CarRentalSystem_API.DTO.StaffDTO;
using CarRentalSystem_API.Function;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrypt;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    [Tags("Admin Staff Management")]
    public class StaffMangementController : Controller
    {
        private readonly AppDbContext _db;
        private static string phonePattern = @"^01[0-9]-\d{7,8}$";
        private static ScryptEncoder encoder = new ScryptEncoder();
        public StaffMangementController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff()
        {
            var staffList = await _db.Users
                .Where(u => u.Role == "Staff")
                .Select(s => new
                {
                    s.UserID,
                    s.UserName,
                    s.Email,
                    s.PhoneNumber,
                    s.Status
                })
                .ToListAsync();
            return Ok(staffList);
        }
        [HttpPost ("createstaff")]
        public async Task<IActionResult> CreateStaff([FromBody] CreateStaffDTO createStaff)
        {
            var existingStaff = await _db.Users.AnyAsync(u => u.Email == createStaff.Email);
            if (existingStaff)
                return BadRequest(new
                {
                    error = "User Already Exists",
                    message = "A user with the provided email already exists. Please use a different email address."
                });
            if (!System.Text.RegularExpressions.Regex.IsMatch(createStaff.PhoneNumber, phonePattern))
                return BadRequest(new
                {
                    error = "Invalid Phone Number",
                    message = "The provided phone number is invalid. Please enter a valid phone number in the format 01X-XXXXXXX or 01X-XXXXXXXX."
                });
            string safeName = createStaff.Name.Length >= 5 ? createStaff.Name.Substring(0, 5) : createStaff.Name.PadRight(5, 'x');
            string password = safeName + GeneralServices.GenerateNumber(4);
            var staff = new User
            {
                UserName = createStaff.Name,
                Email = createStaff.Email,
                PhoneNumber = createStaff.PhoneNumber,
                Password = encoder.Encode(password),
                Status = "Active",
                DriverLicenseNumber = null,
                OTP = null,
                OTPGeneratedAt = null,
                Role = "Staff",
            };
            _db.Users.Add(staff);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Staff Created Successfully, Password auto generated.",
                StaffDetails = new
                {
                    staff.UserID,
                    staff.UserName,
                    staff.Email,
                    staff.PhoneNumber,
                    Password = password
                }
            });
        }
        [HttpPost ("deactivatestaff/{staffid}")]
        public async Task<IActionResult> StaffDowngrade(int staffid)
        {
            var staff = await _db.Users.FindAsync(staffid);
            if (staff == null)
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "No user found with the provided ID. Please check the staff ID and try again."
                });
            staff.Status = "Deactivated";
            staff.Email = $"deactivated_{staff.UserID}_{staff.Email}";
            staff.PhoneNumber = $"000-00000000";
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Staff Deactivated Successfully",
                Details = $"The staff member with ID {staff.UserID}"
            });
        }
    }
}
