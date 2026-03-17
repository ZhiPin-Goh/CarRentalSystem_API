namespace CarRentalSystem_API.Models
{
    public class News
    {
        public int NewsID { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string? CoverImageURL { get; set; }
        public string? Author { get; set; }
        public DateTime PublishedDate { get; set; }
        public bool IsPublished { get; set; } = true;
    }
}
