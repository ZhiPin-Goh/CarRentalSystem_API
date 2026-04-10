using CarRentalSystem_API.DTO.UserDTO;
using CarRentalSystem_API.Function;
using CarRentalSystem_API.Models;
using CloudinaryDotNet;
using Microsoft.AspNetCore.Mvc;
using CarRentalSystem_API.Function;
using Microsoft.EntityFrameworkCore;
using Scrypt;
using System.Security.Claims;
using CloudinaryDotNet.Actions;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Tags("Auth User Management")]
    public class ManageUserController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private static string phonePattern = @"^01[0-9]-\d{7,8}$";
        private static string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$";
        private static string emailPattern = @"^.+@.+$";
        private static ScryptEncoder encoder = new ScryptEncoder();
        public ManageUserController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }
        [HttpGet]
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
        [HttpGet]
        public async Task<IActionResult> UserProfile()
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (userID == 0)
            {
                return BadRequest(new
                {
                    error = "Invalid User ID",
                    message = "User ID is missing or invalid."
                });
            }
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserID == userID);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found.",
                    message = $"User with ID {userID} not found."
                });
              
            }
            return Ok(user);
        }
        // This is create user not authentication the email and password, just create user for admin
        [HttpPost]
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
        [HttpPost]
        public async Task<IActionResult> SignUpUser([FromBody] SignUpDTO createUser)
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
            string otp = GeneralServices.GenerateNumber(6);
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == createUser.Email && u.PhoneNumber == createUser.PhoneNumber);
            if (existingUser != null)
            {
                if (existingUser.Status == "Active")
                {
                    return BadRequest(new
                    {
                        error = "User Already Exists",
                        message = "A user with the same email and phone number already exists."
                    });
                }
                else if (existingUser.Status == "Inactive")
                {
                    return BadRequest(new
                    {
                        error = "User Account Inactive",
                        message = "Your account is inactive. Please contact support for assistance."
                    });
                }
                else if (existingUser.Status == "Pending")
                {
                    existingUser.Password = createUser.Password;
                    existingUser.Password = createUser.Password;
                    existingUser.PhoneNumber = createUser.PhoneNumber;
                    existingUser.OTP = otp;
                    existingUser.OTPGeneratedAt = DateTime.Now;
                    _db.Entry(existingUser).State = EntityState.Modified;
                }
            }
            else
            {
                var newUser = new User
                {
                    UserName = "User" + GeneralServices.GenerateNumber(4),
                    Email = createUser.Email,
                    PhoneNumber = createUser.PhoneNumber,
                    Password = createUser.Password,
                    OTP = otp, // Generate OTP
                    OTPGeneratedAt = DateTime.Now,
                    Status = "Pending",
                    Role = "User"
                };
                await _db.Users.AddAsync(newUser);
            }
            await _db.SaveChangesAsync();
            string emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                </head>
                <body style=""margin: 0; padding: 0; background-color: #f7f8f9; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
    
                    <div style=""width: 100%; background-color: #f7f8f9; padding: 40px 0;"">
        
                        <div style=""max-width: 500px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; box-shadow: 0 8px 24px rgba(0, 0, 0, 0.04); overflow: hidden; border-top: 6px solid #3b82f6;"">
            
                            <div style=""padding: 30px 30px 10px 30px; text-align: left;"">
                                <h1 style=""margin: 0; color: #222222; font-size: 24px; font-weight: bold; letter-spacing: -0.5px;"">
                                    Drive<span style=""color: #3b82f6;"">Link</span>
                                </h1>
                            </div>
            
                            <div style=""padding: 10px 30px 30px 30px; color: #555555; line-height: 1.6;"">
                                <h2 style=""margin: 0 0 20px 0; font-size: 20px; color: #222222;"">
                                    Verify Your Account
                                </h2>
                
                                <p style=""margin: 0 0 15px 0; font-size: 16px;"">
                                    Hi <strong> Dear Customer </strong>,
                                </p>
                                <p style=""margin: 0 0 25px 0; font-size: 15px;"">
                                    Thank you for choosing DriveLink! We're excited to help you find the perfect car. To complete your registration, please use the OTP code below.
                                </p>
                
                                <div style=""background-color: #e0f2fe; border: 2px solid #3b82f6; border-radius: 8px; text-align: center; padding: 25px 20px; margin: 25px 0;"">
                                    <p style=""margin: 0; font-size: 38px; font-weight: bold; color: #3b82f6; letter-spacing: 10px;"">
                                        {otp}
                                    </p>
                                </div>
                
                                <p style=""margin: 0 0 20px 0; font-size: 14px; color: #777777;"">
                                    This OTP is valid for 10 minutes. For your security, please do not share this code with anyone.
                                </p>

                                <p style=""margin: 0; font-size: 13px; color: #999999; border-top: 1px dashed #eeeeee; padding-top: 15px;"">
                                    <strong>Security Notice:</strong> If you did not request this verification, you can safely ignore this email.
                                </p>
                            </div>
            
                            <div style=""background-color: #f9fafa; padding: 20px 30px; text-align: center; border-top: 1px solid #eeeeee;"">
                                <p style=""margin: 0; color: #777777; font-size: 13px;"">Best regards, <br/>The <strong style=""color: #3b82f6;"">DriveLink Team</strong></p>
                                <p style=""margin: 8px 0 0 0; color: #cccccc; font-size: 12px;"">&copy; 2026 DriveLink Global. All rights reserved.</p>
                            </div>
            
                        </div>
                    </div>
                </body>
                </html>";
            try
            {
                await GeneralServices.SendEmail(createUser.Email, "Account Verification OTP", emailBody);
                return Ok(new
                {

                    message = "User created successfully. Please check your email for the OTP to verify your account.",
                    OTP = otp, // For testing purposes, return OTP in response (remove in production)
                    Email = createUser.Email
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex.Message);
                return StatusCode(500, "User created but failed to send OTP email. Please contact support.");
            }
        }
        [HttpPost]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPDTO verify)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == verify.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }
            if (user.Status == "Active")
            {
                return BadRequest(new
                {
                    error = "User Already Active",
                    message = "Your account is already active. Please log in."
                });

            }
            if (user.OTPGeneratedAt.HasValue && user.OTPGeneratedAt.Value.AddMinutes(10) < DateTime.Now)
            {
                return BadRequest(new
                {
                    error = "OTP Expired",
                    message = "OTP has expired. Please request a new OTP."
                });
            }
            if (user.OTP == verify.OTP)
            {
                user.Status = "Active";
                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    message = "OTP verified successfully. Your account is now active.",
                    UserID = user.UserID,
                    Email = user.Email
                });
            }
            else
            {
                return BadRequest(new
                {
                    error = "Invalid OTP",
                    message = "Invalid OTP. Please check your email and try again."
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ResendOTP([FromBody] EmailDTO email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }
            if (user.Status == "Inactive")
            {
                return BadRequest(new
                {
                    error = "User Account Inactive",
                    message = "Your account is inactive. Please contact support for assistance."
                });
            }
            string otp = GeneralServices.GenerateNumber(6);
            user.OTP = otp;
            user.OTPGeneratedAt = DateTime.Now;
            _db.Entry(user).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            string emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                </head>
                <body style=""margin: 0; padding: 0; background-color: #f3f4f6; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
    
                    <div style=""width: 100%; background-color: #f3f4f6; padding: 40px 0;"">
        
                        <div style=""max-width: 500px; margin: 0 auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 4px 20px rgba(0, 0, 0, 0.05); overflow: hidden;"">
            
                            <div style=""background-color: #111827; padding: 25px 30px; text-align: left;"">
                                <h1 style=""margin: 0; color: #ffffff; font-size: 22px; font-weight: bold; letter-spacing: -0.5px; text-transform: uppercase;"">
                                    Drive<span style=""color: #3b82f6;"">Link</span>
                                </h1>
                            </div>
            
                            <div style=""padding: 30px; color: #374151; line-height: 1.6;"">
                
                                <h2 style=""margin: 0 0 20px 0; font-size: 18px; color: #111827;"">
                                    New Verification Code Requested
                                </h2>
                
                                <p style=""margin: 0 0 15px 0; font-size: 15px;"">
                                    Hi <strong> Dear Customer </strong>,
                                </p>
                                <p style=""margin: 0 0 20px 0; font-size: 15px; color: #4b5563;"">
                                    We received a request to resend your OTP. Here is your new Start Code to access your DriveLink account:
                                </p>
                
                                <div style=""background-color: #eff6ff; border-left: 4px solid #3b82f6; border-radius: 0 6px 6px 0; text-align: center; padding: 20px; margin: 25px 0;"">
                                    <p style=""margin: 0 0 8px 0; font-size: 12px; color: #3b82f6; text-transform: uppercase; letter-spacing: 1px; font-weight: bold;"">
                                        Your Start Code
                                    </p>
                                    <p style=""margin: 0; font-size: 40px; font-weight: bold; color: #1e3a8a; letter-spacing: 10px;"">
                                        {otp}
                                    </p>
                                </div>
                
                                <p style=""margin: 0 0 25px 0; font-size: 14px; color: #6b7280;"">
                                    Please enter this code in the app. It will expire in 10 minutes. Do not share this code with anyone.
                                </p>

                                <div style=""background-color: #fef2f2; border: 1px solid #fca5a5; border-radius: 6px; padding: 15px; margin-top: 10px;"">
                                    <p style=""margin: 0; font-size: 13px; color: #991b1b;"">
                                        <strong>⚠️ Security Alert:</strong> If you did not request a new code, someone may be trying to access your account. Please ignore this email or contact our support team immediately.
                                    </p>
                                </div>
                            </div>
            
                            <div style=""background-color: #f9fafb; padding: 20px 30px; text-align: center; border-top: 1px solid #e5e7eb;"">
                                <p style=""margin: 0; color: #6b7280; font-size: 13px;"">Safe travels, <br/><strong style=""color: #111827;"">The DriveLink Team</strong></p>
                            </div>
            
                        </div>
                    </div>
                </body>
                </html>";
            try
            {
                await GeneralServices.SendEmail(user.Email, "Resend OTP for Account Verification", emailBody);
                return Ok(new
                {
                    message = "OTP resent successfully. Please check your email for the new OTP.",
                    Email = user.Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to Send Email",
                    message = "Failed to resend OTP email. Please contact support." + ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ForgetPassword([FromBody] EmailDTO email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }
            string otp = GeneralServices.GenerateNumber(6);
            user.OTP = otp;
            user.OTPGeneratedAt = DateTime.Now;
            _db.Entry(user).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            string emailBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""UTF-8"">
                </head>
                <body style=""margin: 0; padding: 0; background-color: #f3f4f6; font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; -webkit-font-smoothing: antialiased;"">
                    
                    <div style=""width: 100%; background-color: #f3f4f6; padding: 40px 0;"">
                        
                        <div style=""max-width: 500px; margin: 0 auto; background-color: #ffffff; border-radius: 10px; box-shadow: 0 8px 24px rgba(0, 0, 0, 0.06); overflow: hidden; border-top: 6px solid #111827;"">
                            
                            <div style=""padding: 30px 30px 10px 30px; text-align: left;"">
                                <h1 style=""margin: 0; color: #111827; font-size: 24px; font-weight: bold; letter-spacing: -0.5px; text-transform: uppercase;"">
                                    Drive<span style=""color: #3b82f6;"">Link</span>
                                </h1>
                            </div>
                            
                            <div style=""padding: 10px 30px 30px 30px; color: #374151; line-height: 1.6;"">
                                <h2 style=""margin: 0 0 20px 0; font-size: 20px; color: #111827;"">
                                    🔒 Password Reset Request
                                </h2>
                                
                                <p style=""margin: 0 0 15px 0; font-size: 16px;"">
                                    Hi <strong>{user.UserName}</strong>,
                                </p>
                                <p style=""margin: 0 0 25px 0; font-size: 15px; color: #4b5563;"">
                                    We received a request to reset the password for your DriveLink account. Please use the secure authorization code below to proceed.
                                </p>
                                
                                <div style=""background-color: #1e293b; border-radius: 8px; text-align: center; padding: 25px 20px; margin: 30px 0; box-shadow: inset 0 2px 4px rgba(0,0,0,0.1);"">
                                    <p style=""margin: 0 0 10px 0; font-size: 12px; color: #94a3b8; text-transform: uppercase; letter-spacing: 1.5px; font-weight: bold;"">
                                        Authorization Code
                                    </p>
                                    <p style=""margin: 0; font-size: 42px; font-weight: bold; color: #67e8f9; letter-spacing: 12px;"">
                                        {otp}
                                    </p>
                                </div>
                                
                                <p style=""margin: 0 0 20px 0; font-size: 14px; color: #6b7280;"">
                                    Enter this code in the DriveLink app to verify your identity. <strong style=""color: #374151;"">Never share this code with anyone</strong>, including DriveLink staff.
                                </p>

                                <div style=""background-color: #fff1f2; border-left: 4px solid #e11d48; padding: 15px; margin-top: 25px;"">
                                    <p style=""margin: 0; font-size: 13px; color: #9f1239; line-height: 1.5;"">
                                        <strong>Security Action Required:</strong> If you did not request a password reset, your account may be at risk. Please ignore this email and secure your account immediately in the app.
                                    </p>
                                </div>
                            </div>
                            
                            <div style=""background-color: #f9fafb; padding: 20px 30px; text-align: center; border-top: 1px solid #e5e7eb;"">
                                <p style=""margin: 0; color: #6b7280; font-size: 13px;"">DriveLink Security Team</p>
                                <p style=""margin: 8px 0 0 0; color: #d1d5db; font-size: 12px;"">&copy; 2026 DriveLink Global. All rights reserved.</p>
                            </div>
                            
                        </div>
                    </div>
                </body>
                </html>";
            try
            {
                await GeneralServices.SendEmail(user.Email, "Password Reset Authorization Code", emailBody);
                return Ok(new
                {
                    message = "Password reset email sent successfully. Please check your email for the OTP to reset your password.",
                    Email = user.Email
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to Send Email",
                    message = "Failed to send password reset email. Please contact support." + ex.Message
                });
            }
        }
        [HttpPost]
        public async Task<IActionResult> ResetPasswordVerifyOTP([FromBody] VerifyOTPDTO verify)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == verify.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }
            if (user.OTPGeneratedAt.HasValue && user.OTPGeneratedAt.Value.AddMinutes(10) < DateTime.Now)
            {
                return BadRequest(new
                {
                    error = "OTP Expired",
                    message = "OTP has expired. Please request a new OTP."
                });

            }
            else
            {
                if (user.OTP == verify.OTP)
                {
                    return Ok(new
                    {
                        message = "OTP verified successfully. You can now reset your password.",
                        UserID = user.UserID,
                        Email = user.Email
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        error = "Invalid OTP",
                        message = "Invalid OTP. Please check your email and try again."
                    });
                }
            }
        }
        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPassword resetPassword)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == resetPassword.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(resetPassword.NewPassword, passwordPattern))
            {
                return BadRequest(new
                {
                    error = "Invalid Password Format",
                    message = "New password must be at least 8 characters long and include uppercase, lowercase, digit, and special character."
                });
            }
            user.Password = encoder.Encode(resetPassword.NewPassword);
            _db.Entry(user).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Password reset successfully. You can now log in with your new password.",
                UserID = user.UserID,
                Email = user.Email
            });
        }
        [HttpPost]
        public async Task<IActionResult> LoginUser([FromBody] LoginDTO login)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == login.Email);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }

            bool isPasswordValid = encoder.Compare(login.Password, user.Password);
            if (!isPasswordValid)
            {
                return BadRequest(new
                {
                    error = "Invalid Credentials",
                    message = "Incorrect email or password. Please try again."
                });
            }
            if(user.Role == "Staff")
                return BadRequest(new
                {
                    error = "Unauthorized Role",
                    message = "Staff accounts cannot log in through this endpoint. Please use the staff portal."
                });
            if (user.Status != "Active")
            {
                return BadRequest(new
                {
                    error = "Account Not Active",
                    message = "Your account is not active. Please verify your email or contact support."
                });
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_config["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role)
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
                Message = $"Token generated for user {user.Email} with role {user.Role}",
                Token = tokenString,
                AllowAccessToken = TimeSpan.FromDays(1).ToString(),
                UserID = user.UserID,
                Role = user.Role,
            });
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Login successful.",
                Token = tokenString,
                UserID = user.UserID,
                Email = user.Email,
                Role = user.Role
            });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserDTO updateUser)
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (userID == 0)
            {
                return BadRequest(new
                {
                    error = "Invalid User ID",
                    message = "User ID is missing or invalid."
                });
            }
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserID == userID);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(updateUser.PhoneNumber.ToString(), phonePattern))
            {
                return BadRequest(new
                {
                    error = "Invalid Phone Number Format",
                    message = "Invalid phone number format. Format: 01X-XXXXXXX"
                });
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(updateUser.Email, emailPattern))
            {
                return BadRequest(new
                {
                    error = "Invalid Email Format",
                    message = "Invalid email format."
                });
            }
            var existingUser = await _db.Users.AnyAsync(u => u.Email == updateUser.Email && u.PhoneNumber == updateUser.PhoneNumber && u.UserID != userID);
            if (existingUser)
            {
                return BadRequest(new
                {
                    error = "User Already Exists",
                    message = "A user with the same email and phone number already exists."
                });
            }
            user.UserName = updateUser.UserName;
            user.Email = updateUser.Email;
            user.PhoneNumber = updateUser.PhoneNumber;
            _db.Entry(user).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "User updated successfully.",
                UserID = user.UserID,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            });
        }
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword changePassword)
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (userID == 0) 
                return BadRequest(new
                {
                    error = "Invalid User ID",
                    message = "User ID is missing or invalid."
                });
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserID == userID);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }
            var isCurrentPasswordValid = encoder.Compare(changePassword.CurrentPassword, user.Password);
            if (!isCurrentPasswordValid)
            {
                return BadRequest(new
                {
                    error = "Invalid Current Password",
                    message = "Current password is incorrect. Please try again."
                });
            }
            if (!System.Text.RegularExpressions.Regex.IsMatch(changePassword.NewPassword, passwordPattern))
            {
                return BadRequest(new
                {
                    error = "Invalid New Password Format",
                    message = "New password must be at least 8 characters long and include uppercase, lowercase, digit, and special character."
                });
            }
            user.Password = encoder.Encode(changePassword.NewPassword);
            _db.Entry(user).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Password changed successfully.",
                UserID = user.UserID,
                Email = user.Email
            });
        }
        [HttpPost]
        public async Task<IActionResult> UploadDriveInformation([FromForm] UploadLicenseInfoDTO uploadLicense)
        {
            try
            {
                string IcPattern = @"^(\d{2})(0[1-9]|1[0-2])(0[1-9]|[12]\d|3[01])\d{6}$";
                int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                if (userID == 0)
                    return BadRequest(new
                    {
                        error = "Invalid User ID",
                        message = "User ID is missing or invalid."
                    });
                var user = await _db.Users.FirstOrDefaultAsync(x => x.UserID == userID);
                if (user == null)
                    return NotFound(new
                    {
                        error = "User Not Found",
                        message = "User not found."
                    });
                if(!System.Text.RegularExpressions.Regex.IsMatch(uploadLicense.DriverLicenseNumber, IcPattern))
                {
                    return BadRequest(new
                    {
                        error = "Invalid Driver License Number Format",
                        message = "Driver license number must be in the format YYMMDDXXXXXX."
                    });
                }
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        error = "Invalid Input",
                        message = "Please provide valid driver license information."
                    });
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(uploadLicense.DriverLicenseImage.FileName).ToLowerInvariant();
                if(!allowedExtensions.Contains(ext))
                {
                    return BadRequest(new
                    {
                        error = "Invalid File Type",
                        message = "Only JPG, JPEG, and PNG files are allowed for the driver license image."
                    });
                }

                var cloudName = _config["CloudinarySettings:CloudName"];
                var apiKey = _config["CloudinarySettings:ApiKey"];
                var apiSecret = _config["CloudinarySettings:ApiSecret"];
                Account account = new Account(cloudName, apiKey, apiSecret);
                Cloudinary cloudinary = new Cloudinary(account);
                cloudinary.Api.Secure = true;

                var uploadFolder = new ImageUploadResult();
                using(var stream = uploadLicense.DriverLicenseImage.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(uploadLicense.DriverLicenseImage.FileName, stream),
                        Folder = "car_rental_system/DriverLicenses",
                    };
                    uploadFolder = cloudinary.Upload(uploadParams);
                    if (uploadFolder.Error != null)
                    {
                        return StatusCode(500, new
                        {
                            error = "Cloudinary Upload Failed",
                            message = "Failed to upload driver license image. Please try again."
                        });
                    }
                }
                user.DriverLicenseNumber = uploadLicense.DriverLicenseNumber;
                user.DriverLicenseImage = uploadFolder.SecureUrl.ToString();
                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return Ok(new
                {
                    message = "License information uploaded successfully.",
                    UserID = user.UserID,
                    DriverLicenseNumber = user.DriverLicenseNumber,
                    DriverLicenseImageURL = user.DriverLicenseImage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to Upload License Information",
                    message = "An error occurred while uploading license information. Please try again." + ex.Message
                });
            }

        }
        [HttpPost("{id}/toggle-status")]
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
        [HttpPost]
        public async Task<IActionResult> UserLogout()
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (userID == 0)
            {
                return BadRequest(new
                {
                    error = "Invalid User ID",
                    message = "User ID is missing or invalid."
                });
            }
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserID == userID);
            if (user == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            }
            var userToken = await _db.TokenActivities.Where(t => t.UserID == userID && t.Role == user.Role && t.Token != null).ToListAsync();
            userToken.ForEach(t => t.Token = $"This token is invalid as the user logged out at {DateTime.Now}");
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Logout successful. All active tokens for this user have been invalidated.",
                UserID = user.UserID,
                Email = user.Email
            });
        }
        // This is user delete that will be used for user self-deletion. It will set the user status to "Deactivated" and anonymize the email and phone number. The user can no longer log in or use the account, but the data is kept for record-keeping and potential reactivation.
        [HttpPost]
        public async Task<IActionResult> UserDowngrade()
        {
            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (userID == 0)
            {
                return BadRequest(new
                {
                    error = "Invalid User ID",
                    message = "User ID is missing or invalid."
                });
            }
            var user = await _db.Users.FirstOrDefaultAsync(x => x.UserID == userID);
            if(user == null) 
                return NotFound(new
                {
                    error = "User Not Found",
                    message = "User not found."
                });
            user.Status = "Deactivated";
            user.Email = $"User{user.UserID}_{Guid.NewGuid()}@example.com";
            user.PhoneNumber = $"000-0000000";
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "Account downgraded successfully. Your account is now deactivated and your email and phone number have been anonymized.",
                UserID = user.UserID,
                NewStatus = user.Status
            });
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.UserID == id && u.Role != "Staff" && (u.Status == "Deactivated" || u.Status == "Inactive"));
            if (existingUser == null)
            {
                return NotFound(new
                {
                    error = "User Not Found",
                    message = $"User with ID {id} not found."
                });
            }
            _db.Users.Remove(existingUser);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                message = "User deleted successfully.",
                UserID = existingUser.UserID,
                Email = existingUser.Email
            });
        }
    }
}
