namespace CarRentalSystem_API.Models
{
    public class Banners
    {
        public int BannersID { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public string ImageURL { get; set; }
        public string TargetURL { get; set; }
        public int SortOrder { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
    }
}
