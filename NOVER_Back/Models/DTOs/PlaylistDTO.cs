namespace NOVER_Back.Models.DTOs
{
    public class PlaylistDTO
    {
        public int Id { get; set; }

        public int? CreatorId { get; set; }

        public string Title { get; set; } = null!;

        public string? CoverUrl { get; set; }

        public string? Description { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string Type { get; set; } = null!;
    }
}
