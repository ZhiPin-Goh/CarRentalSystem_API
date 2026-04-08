using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CarRentalSystem_API.DTO.AuthTokenDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthTokenController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        public AuthTokenController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        [HttpPost]
        public async Task<IActionResult> GetToken([FromBody] LoginTokenDTO login)
        {
            try
            {
                if (login == null)
                    return BadRequest(new
                    {
                        error = "Invalid Request",
                        message = "Login data is required."
                    });
                string assignedRole;
                int? userId = null;
                if (login.UserName == _config["AdminAccount:UserName"] && login.Password == _config["AdminAccount:Password"])
                {
                    assignedRole = "Admin";
                    userId = null;
                }
                else if (login.UserName == _config["StaffAccount:UserName"] && login.Password == _config["StaffAccount:Password"])
                {
                    assignedRole = "Staff";
                    userId = null;
                }
                else if (login.UserName == _config["CustomerAccount:UserName"] && login.Password == _config["CustomerAccount:Password"])
                {
                    assignedRole = "Customer";
                    userId = null;
                }
                else
                {
                    var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == login.UserName && u.Password == login.Password);
                    if (user == null)
                        return Unauthorized(new
                        {
                            error = "Authentication Failed",
                            message = "Invalid username or password."
                        });
                    assignedRole = user.Role;
                    userId = user.UserID;
                }

                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim("UserID", userId?.ToString() ?? "0"),
                        new Claim(ClaimTypes.Role, assignedRole)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                    Issuer = _config["Jwt:Issuer"],
                    Audience = _config["Jwt:Audience"]
                };

                var rawToken = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(rawToken);

                var token = new TokenActivity
                {
                    Time = DateTime.Now,
                    Message = $"Token generated for role: {assignedRole}, Duration: 1 hour.",
                    Token = tokenString,
                    AllowAccessToken = TimeSpan.FromHours(1).ToString(),
                    AllowRefreshToken = "N/A",
                    UserID = userId,
                    Role = assignedRole
                };

                await _db.TokenActivities.AddAsync(token);
                await _db.SaveChangesAsync();

                return Ok(new TokenResultDTO
                {
                    SuccessToken = tokenString,
                    TokenRole = assignedRole,
                    TokenValidTime = TimeSpan.FromHours(1).ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Internal Server Error",
                    message = ex.Message
                });
            }
        }
        public class TokenResultDTO
        {
            public string SuccessToken { get; set; }
            public string TokenRole { get; set; }
            public string TokenValidTime { get; set; }
        }
        [HttpGet]
        public async Task<IActionResult> GetTokenActivity()
        {
            var activities = await _db.TokenActivities
                .Include(t => t.User)
                .Select(t => new
                {
                    t.ID,
                    t.Token,
                    t.Message,
                    t.Role,
                    t.Time,
                    t.AllowAccessToken,
                    UserName = t.User != null ? t.User.UserName : "N/A"
                })
                .ToListAsync();
            return Ok(activities);
        }
    }
}
