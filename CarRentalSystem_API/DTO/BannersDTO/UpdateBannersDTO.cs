namespace CarRentalSystem_API.DTO.BannersDTO
{
    public class UpdateBannersDTO
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public IFormFile? ImageUrl { get; set; }
        public string TargetUrl { get; set; }
        
    }
}