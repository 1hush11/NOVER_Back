using System.Text.Json.Serialization;

namespace NOVER_Back.Models.DTOs
{
    public class AlbumWithTrackDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int? SingerId { get; set; }

        public string? CoverUrl { get; set; }

        public DateOnly? ReleaseDate { get; set; }

        public virtual SingerDTO? Singer { get; set; }

        public virtual ICollection<TrackDTO> Tracks { get; set; } = new List<TrackDTO>();
    }
}
