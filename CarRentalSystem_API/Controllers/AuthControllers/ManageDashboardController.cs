using CarRentalSystem_API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Controllers.AuthControllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ManageDashboardController : Controller
    {
        private readonly AppDbContext _db;
        public ManageDashboardController(AppDbContext db)
        {
            _db = db;
        }
        [HttpGet]
        public async Task<IActionResult> GetSummary()
        {
            var today = DateTime.Now;
            var tommorrow = today.AddDays(1);
            var beforeOneWeek = today.AddDays(-7);

            //==== Show the KPI of the dashboard ====
            // Today Total Revenue (use sum transactions amount)
            var todayRevenue = await _db.Transactions
                .Where(x => (x.TransactionDate >= today && x.TransactionDate < tommorrow) && x.Status == "Completed" && x.Status != "Refund")
                .SumAsync(x => x.Amount);

            // Now is InProgress Bookings
            var activeBookingsCount = await _db.Bookings
                .CountAsync(x => x.Status == "InProgress");

            // Comming soon bookings
            var unassignTaskCount = await _db.Bookings
                .CountAsync(b => b.AssignedStaffID == null && b.Status == "Confirmed");

            // Usage car
            // 1. Total Vehicles
            var totalVehicles = await _db.Vehicles.CountAsync();
            // 2. Rented Vehicles now
            var rentedVehicles = await _db.Vehicles.CountAsync(v => v.Status == "Rented");
            // 3. Vehicle UtilizationRate
            var UtilizationRate = totalVehicles == 0 ? 0 : Math.Round((double)rentedVehicles / totalVehicles * 100, 1);

            // ==== Show the chart of the dashboard ====
            // Vehicle Status Distribution
            var vehicleStatusBreakdown = await _db.Vehicles
                .GroupBy(v => v.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // 7 days revenue trend
            var pastSummaries = await _db.DailyFinancialSummaries
                .OrderByDescending(s => s.ReportDate)
                .Take(6)
                .OrderBy(s => s.ReportDate)
                .Select(s => new
                {
                    Date = s.ReportDate.ToString("yyyy-MM-dd"),
                    Revenue = s.TotalRevenue
                })
                .ToListAsync();

            // final Trend
            var finalTrend = pastSummaries.ToList<object>();
            finalTrend.Add(new
            {
                Date = today.ToString("yyyy-MM-dd"),
                Revenue = todayRevenue
            });

            var result = new
            {
                // Kpis show in the top of the dashboard
                KPIs = new
                {
                    TodayRevenue = todayRevenue,
                    ActiveBookingsCount = activeBookingsCount,
                    UnassignTaskCount = unassignTaskCount,
                    TotalVehicles = totalVehicles,
                    RentedVehicles = rentedVehicles,
                    UtilizationRate = UtilizationRate
                },
                // Charts show in the bottom of the dashboard
                Charts = new
                {
                    VehicleStatusBreakdown = vehicleStatusBreakdown,
                    RevenueTrend = finalTrend
                }
            };

            return Ok(result);
        }
    }
}
