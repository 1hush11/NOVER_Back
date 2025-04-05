using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NOVER_Back.Models;

public partial class Album
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? SingerId { get; set; }

    public string? CoverUrl { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public virtual Singer? Singer { get; set; }

    public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
}
