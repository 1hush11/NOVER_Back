using System.ComponentModel.DataAnnotations;

namespace NOVER_Back.Models
{
    public class PublishTrackRequest
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;

        public int? AlbumId { get; set; }

        public int? GenreId { get; set; }

        public string? CoverUrl { get; set; }
    }
}
