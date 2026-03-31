using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function.BackgroundServices
{
    public class BannersStatusUpdateServices :BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BannersStatusUpdateServices> _logger;
        public BannersStatusUpdateServices(IServiceProvider serviceProvider, ILogger<BannersStatusUpdateServices> logger)
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
                        var today = DateTime.UtcNow.Date;

                        var bannersToUpdate = await db.Banners
                            .Where(b => b.EndDate < today && b.IsActive == true)
                            .ToListAsync(stoppingToken);

                        foreach (var banner in bannersToUpdate)
                        {
                            banner.IsActive = false;
                        }

                        await db.SaveChangesAsync();
                        _logger.LogInformation($"Banners statuses updated successfully at {DateTime.UtcNow}.");
                    }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while updating banners statuses.");
                }
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
