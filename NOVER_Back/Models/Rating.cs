using System;
using System.Collections.Generic;

namespace NOVER_Back.Models;

public partial class Rating
{
    public int UserId { get; set; }

    public int TrackId { get; set; }

    public int? Rating1 { get; set; }

    public virtual Track Track { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
