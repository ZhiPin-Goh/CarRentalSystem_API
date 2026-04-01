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
            if (login == null)
                return BadRequest(new
                {
                    error = "Invalid Request",
                    message = "Login data is required."
                });
            string assignedRole;
            int userId;
            if (login.UserName == _config["AdminAccount:UserName"] && login.Password == _config["AdminAccount:Password"])
            {
                assignedRole = "Admin";
                userId = 0;
            }
            else
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == login.UserName && u.Password == login.Password);
                if (user == null)
                {
                    if (login.UserName == _config["UserAccount:UserName"] && login.Password == _config["UserAccount:Password"])
                    {
                        assignedRole = "User-Test";
                        userId = 0;
                    }
                    else
                    {
                        return Unauthorized(new
                        {
                            error = "Invalid Credentials",
                            message = "The username or password is incorrect."
                        });
                    }
                }
                assignedRole = "User";
                userId = user.UserID;
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim("UserID", userId.ToString()),
                    new Claim(ClaimTypes.Role, assignedRole)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"]
            };
            var rawToken = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(rawToken);
            _db.TokenActivities.Add(new TokenActivity
            {
                Time = DateTime.UtcNow,
                Message = $"Token generated for userID: {userId}, role: {assignedRole}, Duration: 1 hour.",
                Token = tokenString,
                AllowAccessToken = TimeSpan.FromHours(1).ToString(),
                UserID = userId,
                Role = assignedRole
            });
            await _db.SaveChangesAsync();
            return Ok(new TokenResultDTO
            {
                SuccessToken = tokenString,
                TokenRole = assignedRole,
                TokenValidTime = TimeSpan.FromHours(1).ToString()
            });
        }
        public class TokenResultDTO
        {
            public string SuccessToken { get; set; }
            public string TokenRole { get; set; }
            public string TokenValidTime { get; set; }
        }
    }
}
