namespace CarRentalSystem_API.DTO.PromotionDTO
{
    public class UpdatePromotionDTO
    {
        public int PromotionID { get; set; }
        public string Name { get; set; }
        public string? PromotionCode { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
