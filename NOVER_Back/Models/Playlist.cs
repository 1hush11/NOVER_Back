using System;
using System.Collections.Generic;

namespace NOVER_Back.Models;

public partial class Playlist
{
    public int Id { get; set; }

    public int? CreatorId { get; set; }

    public string Title { get; set; } = null!;

    public string? CoverUrl { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public string Type { get; set; } = null!;

    public virtual User? Creator { get; set; }

    public virtual ICollection<UserPlaylist> UserPlaylists { get; set; } = new List<UserPlaylist>();

    public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
}
