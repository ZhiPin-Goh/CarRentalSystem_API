using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function.BackgroundServices
{
    public class EmptyUserOtpServices : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmptyUserOtpServices> _logger;
        public EmptyUserOtpServices(IServiceProvider serviceProvider, ILogger<EmptyUserOtpServices> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
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
                        var today = DateTime.Now;
                        var emptyOtpUser = await db.Users
                            .Where(x => x.Status == "Active" &&
                            x.OTP != null &&
                            x.OTPGeneratedAt != null &&
                            x.OTPGeneratedAt.Value.AddMinutes(5) < today)
                            .ToListAsync(stoppingToken);
                        foreach (var user in emptyOtpUser)
                        {
                            user.OTP = null;
                            user.OTPGeneratedAt = null;
                        }
                        await db.SaveChangesAsync();
                        _logger.LogInformation($"EmptyUserOtpServices executed at: {DateTime.Now}, empty OTP for {emptyOtpUser.Count} users.");
                    }

                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while emptying user OTPs.");
                }
            }
        }
    }
}
