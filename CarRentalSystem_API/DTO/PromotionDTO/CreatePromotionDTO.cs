namespace CarRentalSystem_API.DTO.PromotionDTO
{
    public class CreatePromotionDTO
    {
        public string Name { get; set; }
        public string? PromotionCode { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public string? PromotionScope { get; set; }
        public decimal? TargetValue { get; set; }
        public string? ApplicableModel { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
