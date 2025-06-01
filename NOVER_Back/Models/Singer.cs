using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NOVER_Back.Models;

public partial class Singer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? PhotoUrl { get; set; }

    public string? Description { get; set; }

    public int? SubscribersCount { get; set; }

    public int? ViewCount { get; set; }

    public string Status { get; set; } = null!;


    [JsonIgnore]
    public virtual ICollection<Album> Albums { get; set; } = new List<Album>();

    [JsonIgnore]
    public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
