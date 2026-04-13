using CarRentalSystem_API.DTO.AuthTokenDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scrypt;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarRentalSystem_API.Controllers.StaffControllers
{
    [ApiController]
    [Route("api/staff/authentication")]
    [Tags("Staff Authentication")]
    public class StaffAuthenticationController : Controller
    {
      private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private static ScryptEncoder encoder = new ScryptEncoder();
        public StaffAuthenticationController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        [AllowAnonymous]
        [HttpPost("stafflogin")]
        public async Task<IActionResult> StaffLogin([FromBody] LoginTokenDTO login)
        {
            try
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

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, staff.UserID.ToString()),
                        new Claim(ClaimTypes.Name, staff.UserName),
                        new Claim(ClaimTypes.Role, staff.Role)
                    }),
                    Expires = DateTime.Now.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _config["Jwt:Issuer"],
                    Audience = _config["Jwt:Audience"]
                };

                var rawToken = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(rawToken);
                var refreshToken = Guid.NewGuid().ToString();

                var token = new TokenActivity
                {
                    Time = DateTime.Now,
                    Message = $"Token generated for user {staff.UserName} with role {staff.Role}",
                    Token = tokenString,
                    AllowAccessToken = TimeSpan.FromHours(1).ToString(),
                    AllowRefreshToken = refreshToken,
                    UserID = staff.UserID,
                    Role = staff.Role
                };
                await _db.TokenActivities.AddAsync(token);
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    token = tokenString,
                    TokenRole = "Admin",
                    refreshToken = refreshToken,
                    expiresIn = TimeSpan.FromHours(1).TotalSeconds
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred during authentication",
                    message = ex.Message
                });
            }
        }
        [AllowAnonymous]
        [HttpPost("staffrefresh")]
        public async Task<IActionResult> RefrenshToken([FromBody] RefreshTokenRequestDTO refreshToken)
        {
            var activity = await _db.TokenActivities.FirstOrDefaultAsync(t => t.AllowRefreshToken == refreshToken.RefreshToken && t.Token == refreshToken.AccessToken);
            if (activity == null)
                return BadRequest(new
                {
                    error = "Invalid refresh token",
                    message = "The provided refresh token is invalid or does not match the access token."
                });
            if (activity.Time.AddDays(7) < DateTime.Now)
                return BadRequest(new
                {
                    error = "Refresh token expired",
                    message = "The provided refresh token has expired. Please log in again."
                });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, activity.UserID?.ToString() ?? "0"),
                    new Claim(ClaimTypes.Role, activity.Role)
                }),
                Expires = DateTime.Now.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };
            var newAccessToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
            var newRefreshToken = Guid.NewGuid().ToString("N");

            activity.Token = newAccessToken;
            activity.AllowRefreshToken = newRefreshToken;
            activity.Time = DateTime.Now;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                token = newAccessToken,
                refreshToken = newRefreshToken,
                expiresIn = TimeSpan.FromHours(1).TotalSeconds
            });
        }
        [HttpPost("stafflogout")]
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
    }
}
