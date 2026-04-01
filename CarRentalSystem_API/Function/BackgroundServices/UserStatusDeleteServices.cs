using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function.BackgroundServices
{
    public class UserStatusDeleteServices : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UserStatusDeleteServices> _logger;
        public UserStatusDeleteServices(IServiceProvider serviceProvider, ILogger<UserStatusDeleteServices> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        // Automatically deletes users status is "Pending"
        protected override async Task ExecuteAsync(CancellationToken stoppigToken)
        {
            while (!stoppigToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        var pendingUsers = await db.Users.Where(u => u.Status == "Pending" &&
                        u.OTPGeneratedAt != null && u.OTPGeneratedAt.Value.AddMinutes(10) < DateTime.Now)
                            .ToListAsync();

                        foreach (var user in pendingUsers)
                        {
                            db.Users.Remove(user);
                        }
                        await db.SaveChangesAsync();
                        _logger.LogInformation("Pending users deleted successfully at " + DateTime.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while deleting pending users.");
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppigToken);
            }
        }
    }
}