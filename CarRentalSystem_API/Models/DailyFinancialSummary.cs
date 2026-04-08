using System.ComponentModel.DataAnnotations;

namespace CarRentalSystem_API.Models
{
    public class DailyFinancialSummary
    {
        [Key]
        public int SummaryID { get; set; }
        [DataType(DataType.Date)]
        public DateTime ReportDate { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalRefund { get; set; }
        public int NewBookingsCount { get; set; }
        public int CompletedHandoversCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
