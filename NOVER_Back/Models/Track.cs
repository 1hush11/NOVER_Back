using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NOVER_Back.Models;

public partial class Track
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? AlbumId { get; set; }

    public int Duration { get; set; }

    public int? GenreId { get; set; }

    public DateOnly? ReleaseDate { get; set; }

    public int? PlayCount { get; set; }

    public string AudioUrl { get; set; } = null!;

    public string? CoverUrl { get; set; }

    public string? Status { get; set; }

    [JsonIgnore]

    public virtual Album? Album { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual Genre? Genre { get; set; }

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();

    public virtual ICollection<Singer> Singers { get; set; } = new List<Singer>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
