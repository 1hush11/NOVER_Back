using System;
using System.Collections.Generic;

namespace NOVER_Back.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? Avatar { get; set; }

    public DateTime? RegistrationDate { get; set; }

    public string Role { get; set; } = null!;

    public string? Status { get; set; }

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual ICollection<Playlist> Playlists { get; set; } = new List<Playlist>();

    public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    public virtual ICollection<UserPlaylist> UserPlaylists { get; set; } = new List<UserPlaylist>();

    public virtual ICollection<Singer> Singers { get; set; } = new List<Singer>();

    public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
}
