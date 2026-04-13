namespace CarRentalSystem_API.DTO.PromotionDTO
{
    public class UpdatePromotionDTO
    {
        public int PromotionID { get; set; }
        public string? Name { get; set; }
        public DateTime EndDate { get; set; }
    }
}
