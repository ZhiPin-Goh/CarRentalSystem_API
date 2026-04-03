namespace CarRentalSystem_API.Models
{
    public class Promotion
    {
        public int PromotionID { get; set; }
        public string Name { get; set; }
        public string? PromotionCode { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public string PromotionScope { get; set; } = "All"; 
        public string? TargetValue { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
