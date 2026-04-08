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
        public DbSet<TokenActivity> TokenActivities { get; set; }
        public DbSet<HandoverReport> HandoverReports { get; set; }
        public DbSet<DeliveryArea> DeliveryAreas { get; set; }
        public DbSet<DailyFinancialSummary> DailyFinancialSummaries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<HandoverReport>()
                .HasOne(h => h.Staff)
                .WithMany()
                .HasForeignKey(h => h.StaffID)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
