using CarRentalSystem_API.Models;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Function.BackgroundServices
{
    public class DailyFinancialJobServices : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyFinancialJobServices> _logger;
        public DailyFinancialJobServices(IServiceProvider serviceProvider, ILogger<DailyFinancialJobServices> logger)
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
                        var today = DateTime.Now;
                        var nextRunTime = new DateTime(today.Year, today.Month, today.Day, 23, 59, 0);
                        if(today > nextRunTime)
                        {
                            nextRunTime = nextRunTime.AddDays(1);
                        }

                        var delay = nextRunTime - today;    
                        _logger.LogInformation("Daily financial job will run at {time}", nextRunTime);
                        await Task.Delay(delay, stoppingToken);

                        await ProcessDailySummary();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while executing the daily financial job at {time}", DateTimeOffset.Now);
                }
            }
        }
        private async Task ProcessDailySummary()
        {
            _logger.LogInformation("Starting daily financial summary processing ");
            using(var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var today = DateTime.Now;
                var tommorrow = today.AddDays(1);

                // Calculate today revenue (sum of transactions with status "Completed")
                var totalRevenue = await db.Transactions
                    .Where(t => t.TransactionDate >= today && t.TransactionDate < tommorrow && t.Status != "Completed")
                    .SumAsync(t => t.Amount);

                // Calculate today refund (sum of transactions with status "Refunded")
                var totalRefund = await db.Transactions
                    .Where(t => t.TransactionDate >= today && t.TransactionDate < tommorrow && t.Status == "Refunded")
                    .SumAsync(t => t.Amount);

                // Count new bookings today
                var newBookingsCount = await db.Bookings
                    .Where(b => b.CreatedAt >= today && b.CreatedAt < tommorrow)
                    .CountAsync();

                // Count completed handovers today
                var completedHandoversCount = await db.HandoverReports
                    .CountAsync(b => b.HandoverTime >= today && b.HandoverTime < tommorrow);

                // Check if a summary for today already exists
                var existingSummary = await db.DailyFinancialSummaries
                    .FirstOrDefaultAsync(s => s.ReportDate == today.Date);

                if (existingSummary != null)
                {
                    existingSummary.ReportDate = today;
                    existingSummary.TotalRevenue = totalRevenue;
                    existingSummary.TotalRefund = totalRefund;
                    existingSummary.NewBookingsCount = newBookingsCount;
                    existingSummary.CompletedHandoversCount = completedHandoversCount;
                    existingSummary.LastUpdated = DateTime.Now;
                }
                else
                {
                    var summary = new DailyFinancialSummary
                    {
                        ReportDate = today,
                        TotalRevenue = totalRevenue,
                        TotalRefund = totalRefund,
                        NewBookingsCount = newBookingsCount,
                        CompletedHandoversCount = completedHandoversCount,
                        LastUpdated = DateTime.Now
                    };
                    db.DailyFinancialSummaries.Add(summary);
                }
                await db.SaveChangesAsync();
                _logger.LogInformation($"Daily financial summary processing completed at {DateTimeOffset.Now}");

            }

        }
    }
}
