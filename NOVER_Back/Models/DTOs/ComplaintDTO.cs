namespace NOVER_Back.Models.DTOs
{
    public class ComplaintDTO
    {
        public int Id { get; set; }

        public int? TrackId { get; set; }

        public string Content { get; set; } = null!;
    }
}
