using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function.BackgroundServices
{
    public class BookingTimeoutServices : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingTimeoutServices> _logger;
        public BookingTimeoutServices(IServiceProvider serviceProvider, ILogger<BookingTimeoutServices> logger)
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
                        var minuteAgo = DateTime.Now.AddMinutes(-15);
                        var expiredBookings = await db.Bookings
                            .Where(b => b.Status == "Pending" && b.CreatedAt <= minuteAgo)
                            .ToListAsync(stoppingToken);
                        if (expiredBookings.Any())
                        {
                            foreach (var booking in expiredBookings)
                            {
                                booking.Status = "Cancelled";
                            }
                            await db.SaveChangesAsync(stoppingToken);
                            if (expiredBookings.Count > 0)
                            {
                                _logger.LogInformation($"Cancelled {expiredBookings.Count} expired bookings.");
                            }
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Booking timeout check completed.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking for expired bookings.");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
