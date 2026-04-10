using CarRentalSystem_API.DTO.AuthTokenDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CarRentalSystem_API.Controllers.AdminControllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Tags("Admin Authentication")]
    public class AdminManagementController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public AdminManagementController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        [HttpPost("adminlogin")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginTokenDTO login)
        {
            try
            {
                var adminName = _config["AdminAccount:UserName"];
                var adminPassword = _config["AdminAccount:Password"];
                if (login.UserName != adminName && login.Password != adminPassword)
                {
                    return BadRequest(new
                    {
                        error = "Authentication failed",
                        message = "Invalid username or password"
                    });
                }
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "0"),
                        new Claim(ClaimTypes.Name, login.UserName),
                        new Claim(ClaimTypes.Role, "Admin")
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
                    Message = $"Token generated for role: Admin, Duration: {TimeSpan.FromHours(1)}",
                    Token = tokenString,
                    AllowAccessToken = TimeSpan.FromHours(1).ToString(),
                    AllowRefreshToken = refreshToken,
                    UserID = null,
                    Role = "Admin"
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
        [HttpPost("adminrefresh")]
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
        [Authorize(Roles = "Admin")]
        [HttpPost("adminlogout")]
        public async Task<IActionResult?> AdminLogout([FromBody] RefreshTokenRequestDTO logoutDTO)
        {
            var activity = await _db.TokenActivities.FirstOrDefaultAsync(t => t.AllowRefreshToken == logoutDTO.RefreshToken && t.Token == logoutDTO.AccessToken);
            if (activity == null)
                return BadRequest(new
                {
                    error = "Invalid token",
                    message = "The provided token is invalid or does not exist."
                });
            activity.Token = $"Token invalidated at {DateTime.Now}";
            activity.AllowRefreshToken = $"Token invalidated at {DateTime.Now}";
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Logout successful"
            });
        }
    }
}
