namespace CarRentalSystem_API.DTO.BannersDTO
{
    public class CreateBannersDTO
    {
        public string Title { get; set; }
        public string? Description { get; set; }
        public IFormFile ImageUrl { get; set; }
        public string TargetUrl { get; set; }
        public int SortOrder { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}