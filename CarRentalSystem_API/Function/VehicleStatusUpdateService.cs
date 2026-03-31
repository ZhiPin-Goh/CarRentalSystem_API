using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function
{
    public class VehicleStatusUpdateService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<VehicleStatusUpdateService> _logger;
        public VehicleStatusUpdateService(IServiceProvider serviceProvider, ILogger<VehicleStatusUpdateService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        // Automatically updates vehicle status based on maintenance records daily
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var today = DateTime.UtcNow.Date;

                        var vehiclesToMaintenance = await db.MaintenanceRecords
                            .Where(m => m.StartDate == today)
                            .Select(m => m.Vehicle)
                            .ToListAsync();

                        foreach (var vehicle in vehiclesToMaintenance)
                        {
                            vehicle.Status = "Maintenance";
                        }

                        var vehiclesToAvailable = await db.MaintenanceRecords
                            .Where(m => m.EndDate == today)
                            .Select(m => m.Vehicle)
                            .ToListAsync();
                        foreach (var vehicle in vehiclesToAvailable)
                        {
                            vehicle.Status = "Available";
                        }

                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating vehicle statuses.");
                return;
            }
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
