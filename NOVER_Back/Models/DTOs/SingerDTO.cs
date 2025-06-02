using System.Text.Json.Serialization;

namespace NOVER_Back.Models.DTOs
{
    public class SingerDTO
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? PhotoUrl { get; set; }

        public string? Description { get; set; }

        public int? ViewCount { get; set; }

        public int? SubscribersCount { get; set; }

        public string Status { get; set; } = null!;
    }
}
