using CarRentalSystem_API.DTO.UserDTO;
using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using CarRentalSystem_API.Function;
using Microsoft.EntityFrameworkCore;
using Scrypt;

namespace CarRentalSystem_API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManageUserController : Controller
    {
        private readonly AppDbContext _db;
        private static string phonePattern = @"^01[0-9]-\d{7,8}$";
        private static string passwordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$";
        private static string emailPattern = @"^.+@.+$";
        ScryptEncoder encoder = new ScryptEncoder();
        public ManageUserController(AppDbContext db)
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
        // This is create user not authentication the email and password, just create user for admin
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
        [HttpPost]
        public async Task<IActionResult> SignUpUser([FromBody] CreateUserDTO createUser)
            {

            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.PhoneNumber.ToString(), phonePattern))
                return BadRequest("Invalid phone number format. Format: 01X-XXXXXXX");
            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.Password, passwordPattern))
                return BadRequest("Password must be at least 8 characters long and include uppercase, lowercase, digit, and special character.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(createUser.Email, emailPattern))
                return BadRequest("Invalid email format.");
            string otp = GeneralServices.GenerateNumber(6);
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == createUser.Email && u.PhoneNumber == createUser.PhoneNumber);
            if (existingUser != null)
            {
                if (existingUser.Status == "Active")
                    return BadRequest("An account with the same email or phone number already exists.");
                else if (existingUser.Status == "Inactive")
                    return BadRequest("An account with the same email or phone number already exists but is inactive. Please contact support.");
                else if (existingUser.Status == "Pending")
                {
                    existingUser.UserName = createUser.UserName;
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
                    UserName = createUser.UserName,
                    Email = createUser.Email,
                    PhoneNumber = createUser.PhoneNumber,
                    Password = createUser.Password,
                    OTP = otp, // Generate OTP
                    OTPGeneratedAt = DateTime.Now,
                    Status = "Pending",
                };
                _db.Users.Add(newUser);
            }
           // await _db.SaveChangesAsync();
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
                                    Hi <strong>{createUser.UserName}</strong>,
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

                    Message = "User created successfully. Please check your email for the OTP to verify your account.",
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
                return NotFound("User not found.");
            if (user.Status == "Active")
                return BadRequest("User is already verified and active.");
            if (user.OTPGeneratedAt.HasValue && user.OTPGeneratedAt.Value.AddMinutes(10) < DateTime.Now)
                return BadRequest("OTP has expired. Please request a new OTP.");

            if (user.OTP == verify.OTP)
            {
                user.Status = "Active";
                _db.Entry(user).State = EntityState.Modified;
                await _db.SaveChangesAsync();
                return Ok("OTP verified successfully.");
            }
            else
            {
                return BadRequest("Invalid OTP. Please check your email and try again.");
            }
        }
        [HttpPost]
        public async Task<IActionResult> ResendOTP([FromBody] EmailDTO email)
        {
            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email.Email);
            if (user == null)
                return NotFound("User not found.");
            if (user.Status == "Inactive")
                return BadRequest("User account is inactive. Please contact support");
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
                                    Hi <strong>{user.UserName}</strong>,
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
                return Ok("OTP resent successfully. Please check your email for the new OTP.");
            }
            catch(Exception ex)
            {
                return StatusCode(500, "Failed to resend OTP email. Please contact support." + ex.Message);
            }
        }

    }
}
