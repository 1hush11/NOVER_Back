using System;
using System.Collections.Generic;

namespace NOVER_Back.Models;

public partial class Complaint
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? TrackId { get; set; }

    public string Content { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual Track? Track { get; set; }

    public virtual User? User { get; set; }
}
