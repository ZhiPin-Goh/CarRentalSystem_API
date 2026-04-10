using CarRentalSystem_API.DTO.StaffDTO;
using CarRentalSystem_API.DTO.UserDTO;
using CarRentalSystem_API.Function;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scrypt;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Tags("Auth Staff Management")]
    public class ManageStaffController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private static string phonePattern = @"^01[0-9]-\d{7,8}$";
        private static ScryptEncoder encoder = new ScryptEncoder();
        public ManageStaffController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        [HttpPost]
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
                TelegramID = null,
                OTP = null,
                OTPGeneratedAt = null,
                Role = "Staff",
            };
            _db.Users.Add(staff);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Staff Created Successfully",
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
        [HttpPost]
        public async Task<IActionResult> StaffLogin([FromBody] StaffLoginDTO login)
        {
            var staff = await _db.Users.FirstOrDefaultAsync(x => x.UserName == login.UserName);
            if (staff == null)
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "No user found with the provided username. Please check the username and try again."
                });
            bool isPasswordValid = encoder.Compare(login.Password, staff.Password);
            if (!isPasswordValid)
                return BadRequest(new
                {
                    error = "Invalid Password",
                    message = "The password you entered is incorrect. Please try again with the correct password."
                });
            if (staff.Role != "Staff")
                return BadRequest(new
                {
                    error = "Unauthorized Access",
                    message = "You do not have permission to access this resource. Please log in with a staff account."
                });
            if (staff.Status != "Active")
                return BadRequest(new
                {
                    error = "Account Inactive",
                    message = "Your account is currently inactive. Please contact the administrator for assistance."
                });

            var tokenhandler = new JwtSecurityTokenHandler();
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, staff.UserID.ToString()),
                    new Claim(ClaimTypes.Email, staff.Email),
                    new Claim(ClaimTypes.Role, staff.Role)
                }),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };
            var rawtoken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(rawtoken);

            _db.TokenActivities.Add(new TokenActivity
            {
                Time = DateTime.Now,
                Message = $"Token generated for Staff Login: {staff.UserName}",
                Token = tokenString,
                AllowAccessToken = TimeSpan.FromDays(1).ToString(),
                UserID = staff.UserID,
                Role = staff.Role,
            });
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Login Successful",
                Token = tokenString,
                StaffDetails = new
                {
                    staff.UserID,
                    staff.UserName,
                    staff.Email,
                    staff.PhoneNumber,
                }
            });
        }
        [HttpPost]
        public async Task<IActionResult> StaffLogout()
        {
            int staffID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (staffID == 0)
                return BadRequest(new
                {
                    error = "Invalid User",
                    message = "Unable to identify the user. Please ensure you are logged in and try again."
                });

            var staff = await _db.Users.FindAsync(staffID);
            if (staff == null)
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "No user found with the provided ID. Please check your login status and try again."
                });
            var token = await _db.TokenActivities.Where(x => x.UserID == staffID && x.Role == staff.Role && x.Token != null).ToListAsync();
            token.ForEach(x => x.Token = $"This token is invalidated at {DateTime.Now}. User logged out.");
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Logout Successful",
                Details = "Your session has been successfully terminated. All active tokens have been invalidated. Please log in again to access your account."
            });
        }
        [HttpPost("{staffid}")]
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
