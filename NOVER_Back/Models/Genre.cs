using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NOVER_Back.Models;

public partial class Genre
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? CoverUrl { get; set; }

    [JsonIgnore]
    public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
}
