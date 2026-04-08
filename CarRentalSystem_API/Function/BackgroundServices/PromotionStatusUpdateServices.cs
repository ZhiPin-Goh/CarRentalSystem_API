using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function.BackgroundServices
{
    public class PromotionStatusUpdateServices : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PromotionStatusUpdateServices> _logger;
        public PromotionStatusUpdateServices(IServiceProvider serviceProvider, ILogger<PromotionStatusUpdateServices> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using(var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var expiredPromotions = await db.Promotions
                        .Where(p => p.EndDate < DateTime.Now && p.IsActive == true)
                        .ToListAsync(stoppingToken);
                    foreach(var promotion in expiredPromotions)
                    {
                        promotion.IsActive = false;
                    }
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Promotion status updated successfully at {time}", DateTimeOffset.Now);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating promotion status at {time}", DateTimeOffset.Now);
            }
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
