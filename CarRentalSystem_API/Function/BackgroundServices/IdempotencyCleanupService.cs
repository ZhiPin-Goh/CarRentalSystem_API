using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function.BackgroundServices
{
    public class IdempotencyCleanupService :BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<IdempotencyCleanupService> _logger;
        public IdempotencyCleanupService(IServiceProvider serviceProvider, ILogger<IdempotencyCleanupService> logger)
        {
            this._serviceProvider = serviceProvider;
            this._logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var sevenDaysAgo = DateTime.Now.AddDays(-7);
                        var oldRecords = await db.Idempotencies
                            .Where(x => x.CreatedAt < sevenDaysAgo && (x.Status == "Completed" || x.Status == "Failed"))
                            .ToListAsync(stoppingToken);
                        if (oldRecords.Any())
                        {
                            db.Idempotencies.RemoveRange(oldRecords);
                            _logger.LogInformation($"Cleaned up {oldRecords.Count} old idempotency records.");
                        }

                        var fifteenMinsAgo = DateTime.Now.AddMinutes(-15);
                        var zombieRecords = await db.Idempotencies
                            .Where(x => x.CreatedAt < fifteenMinsAgo && x.Status == "Started")
                            .ToListAsync(stoppingToken);

                        if (zombieRecords.Any())
                        {
                            db.Idempotencies.RemoveRange(zombieRecords);
                            _logger.LogWarning($"Cleaned up {zombieRecords.Count} ZOMBIE idempotency records stuck in 'Started'.");
                        }
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while cleaning up idempotency records.");
                }
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
