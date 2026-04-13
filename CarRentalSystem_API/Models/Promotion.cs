namespace CarRentalSystem_API.Models
{
    public class Promotion
    {
        public int PromotionID { get; set; }
        public string Name { get; set; }
        public string? PromotionCode { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public string PromotionScope { get; set; } = "All"; //Promotion Type: Global, ModelSpecific, MinDuration, MinSpend
        public decimal? TargetValue { get; set; } //Days: e.g 3; MinSpend e.g 100....
        public string? ApplicableModel { get; set; } // For ModelSpecific the model name
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
