using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function.BackgroundServices
{
    public class TripRemindersServices :BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TripRemindersServices> _logger;
        public TripRemindersServices(IServiceProvider serviceProvider, ILogger<TripRemindersServices> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var upcomingTrips = await db.Bookings
                            .Include(b => b.User)
                            .Include(b => b.Vehicle)
                            .Where(b => b.StartDate > DateTime.UtcNow && b.StartDate <= DateTime.Now.AddDays(24) && b.Status == "Confirmed")
                            .ToListAsync(stoppingToken);

                        foreach(var booking in upcomingTrips)
                        {
                            string reminderEmailBody = $@"
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
                                                Your Trip is Tomorrow! 🚗
                                            </h2>

                                            <p style=""margin: 0 0 15px 0; font-size: 16px;"">
                                                Hi <strong>{booking.User.UserName}</strong>,
                                            </p>
                                            <p style=""margin: 0 0 20px 0; font-size: 15px;"">
                                                Get ready to hit the road! Your reserved vehicle will be ready for you tomorrow. Here is a quick summary of your booking:
                                            </p>

                                            <div style=""background-color: #e0f2fe; border-left: 4px solid #3b82f6; border-radius: 8px; padding: 20px; margin: 25px 0;"">
                                                <table style=""width: 100%; font-size: 15px; color: #222222; border-collapse: collapse;"">
                                                    <tr>
                                                        <td style=""padding-bottom: 10px; color: #555555; width: 40%;"">Booking ID:</td>
                                                        <td style=""padding-bottom: 10px; font-weight: bold;"">#{booking.BookingID}</td>
                                                    </tr>
                                                    <tr>
                                                        <td style=""padding-bottom: 10px; color: #555555;"">Vehicle:</td>
                                                        <td style=""padding-bottom: 10px; font-weight: bold;"">{booking.Vehicle.Model} ({booking.Vehicle.LicensePlate})</td>
                                                    </tr>
                                                    <tr>
                                                        <td style=""padding-bottom: 10px; color: #555555;"">Date & Time:</td>
                                                        <td style=""padding-bottom: 10px; font-weight: bold;"">{booking.StartDate.ToString("dd MMM yyyy")}</td>
                                                    </tr>
                                                    <tr>
                                                        <td style=""padding-bottom: 10px; color: #555555;"">Handover:</td>
                                                        <td style=""padding-bottom: 10px; font-weight: bold;"">{booking.HandoverMethod}</td>
                                                    </tr>
                                                    <tr>
                                                        <td style=""color: #555555;"">Location:</td>
                                                        <td style=""font-weight: bold;"">{booking.DeliveryAddress}</td>
                                                    </tr>
                                                </table>
                                            </div>

                                            <p style=""margin: 0 0 20px 0; font-size: 14px; color: #555555;"">
                                                <strong>Important Reminder:</strong> Please ensure you have your physical driver's license ready during the handover process. Our staff will assist you with the check-out inspection.
                                            </p>

                                            <p style=""margin: 0; font-size: 13px; color: #999999; border-top: 1px dashed #eeeeee; padding-top: 15px;"">
                                                If you need to make any urgent changes or need help finding the location, please contact our support team immediately.
                                            </p>
                                        </div>

                                        <div style=""background-color: #f9fafa; padding: 20px 30px; text-align: center; border-top: 1px solid #eeeeee;"">
                                            <p style=""margin: 0; color: #777777; font-size: 13px;"">Have a safe drive! <br/>The <strong style=""color: #3b82f6;"">DriveLink Team</strong></p>
                                            <p style=""margin: 8px 0 0 0; color: #cccccc; font-size: 12px;"">&copy; 2026 DriveLink Global. All rights reserved.</p>
                                        </div>

                                    </div>
                                </div>
                            </body>
                            </html>";
                            await GeneralServices.SendEmail(booking.User.Email, "Your Trip is Tomorrow! 🚗", reminderEmailBody);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while sending trip reminders.");
                }
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
