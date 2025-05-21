using System;
using System.Collections.Generic;

namespace NOVER_Back.Models;

public partial class Comment
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int TrackId { get; set; }

    public string CommentText { get; set; } = null!;

    public DateTime? CreatedAt { get; set; }

    public string? Status { get; set; }

    public virtual Track Track { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
