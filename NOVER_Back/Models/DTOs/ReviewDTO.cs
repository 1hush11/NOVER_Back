namespace NOVER_Back.Models.DTOs
{
    public class ReviewDTO
    {
        public int TrackId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
