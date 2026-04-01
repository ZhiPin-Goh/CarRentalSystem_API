namespace CarRentalSystem_API.DTO.NewsDTO
{
    public class CreateNewsDTO
    {
        public string Title { get; set; }
        public string Summary { get; set; }
        public string Content { get; set; }
        public string? CoverImageURL { get; set; }
        public string? Author { get; set; }
    }
}
