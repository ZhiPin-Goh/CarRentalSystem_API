namespace CarRentalSystem_API.DTO.NewsDTO
{
    public class UpdateNewDTO
    {
        public int NewsID { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string? CoverImageURL { get; set; }
        public string? Author { get; set; }
    }
}
