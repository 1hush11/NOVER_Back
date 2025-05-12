namespace NOVER_Back.Models
{
    public class PublishAlbumRequest
    {
        public string AlbumName { get; set; } = null!;
        public string? CoverUrl { get; set; }
        public int? GenreId { get; set; }
    }
}
