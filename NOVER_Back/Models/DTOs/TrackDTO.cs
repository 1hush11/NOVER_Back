namespace NOVER_Back.Models.DTOs
{
    public class TrackDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int? AlbumId { get; set; }
        public string? AlbumTitle { get; set; }

        public int Duration { get; set; }

        public string? GenreName { get; set; }
        public int? GenreId { get; set; }

        public DateOnly? ReleaseDate { get; set; }

        public int? PlayCount { get; set; }

        public string AudioUrl { get; set; } = null!;

        public string? CoverUrl { get; set; }

        public string? Status { get; set; }
        public List<SingerDTO> Singers { get; set; } = new();
    }
}
