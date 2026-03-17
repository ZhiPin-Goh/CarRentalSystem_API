using Microsoft.EntityFrameworkCore;

namespace CarRentalSystem_API.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<Banners> Banners { get; set; }
        public DbSet<News> News { get; set; }
        public DbSet<VehicleImage> VehicleImages { get; set; }
    }
}
