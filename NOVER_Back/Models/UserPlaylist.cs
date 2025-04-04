using System;
using System.Collections.Generic;

namespace NOVER_Back.Models;

public partial class UserPlaylist
{
    public int UserId { get; set; }

    public int PlaylistId { get; set; }

    public bool? IsOwner { get; set; }

    public virtual Playlist Playlist { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
