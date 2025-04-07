using Microsoft.AspNetCore.Mvc;
using NOVER_Back.Models.DTOs;
using NOVER_Back.Models;
using Microsoft.EntityFrameworkCore;

namespace NOVER_Back.Controllers
{
    [Route("api/playlist")]
    [ApiController]
    public class PlaylistController : ControllerBase
    {
        private readonly NoverDbContext _context;

        public PlaylistController(NoverDbContext context)
        {
            _context = context;
        }

        [HttpGet("playlists/{id}")]
        public async Task<ActionResult<object>> GetPlaylistById(int id)
        {
            var playlist = await _context.Playlists
                .Include(p => p.Tracks)
                    .ThenInclude(t => t.Singers)
                .Include(p => p.Creator)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (playlist == null)
                return NotFound("Плейлист не найден.");

            var currentUserId = GetCurrentUserId();
            var isOwner = currentUserId != null && playlist.CreatorId == currentUserId;

            var savedCount = await _context.UserPlaylists
                .CountAsync(up => up.PlaylistId == id && (up.IsOwner ?? false) == false);

            return Ok(new
            {
                playlist.Id,
                playlist.Title,
                playlist.CoverUrl,
                playlist.Description,
                playlist.CreatedAt,
                playlist.Type,
                Creator = playlist.Creator?.Username,
                Tracks = playlist.Tracks.Select(t => new TrackDTO
                {
                    Id = t.Id,
                    Name = t.Name,
                    AlbumId = t.AlbumId,
                    Duration = t.Duration,
                    GenreId = t.GenreId,
                    ReleaseDate = t.ReleaseDate,
                    PlayCount = t.PlayCount,
                    AudioUrl = t.AudioUrl,
                    CoverUrl = t.CoverUrl,
                    Status = t.Status,
                    Singers = t.Singers.Select(s => s.Name).ToList()
                }).ToList(),
                savedCount,
                isOwner
            });
        }

        [HttpPost("add_playlist")]
        public async Task<ActionResult> CreatePlaylist([FromBody] PlaylistDTO playlist)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Не авторизован.");

            var newPlaylist = new Playlist
            {
                Title = playlist.Title,
                CoverUrl = playlist.CoverUrl,
                Description = playlist.Description,
                Type = playlist.Type,
                CreatedAt = DateTime.UtcNow,
                CreatorId = userId
            };

            _context.Playlists.Add(newPlaylist);
            await _context.SaveChangesAsync();

            _context.UserPlaylists.Add(new UserPlaylist
            {
                PlaylistId = playlist.Id,
                UserId = userId.Value,
                IsOwner = true
            });

            await _context.SaveChangesAsync();

            return Ok(new { playlist.Id });
        }

        [HttpPost("playlists/{id}/add")]
        public async Task<IActionResult> AddTrackToPlaylist(int id, [FromBody] int trackId)
        {
            var playlist = await _context.Playlists
                .Include(p => p.Tracks)
                .FirstOrDefaultAsync(p => p.Id == id);

            var track = await _context.Tracks.FindAsync(trackId);

            if (playlist == null || track == null)
                return NotFound("Плейлист или трек не найден.");

            if (playlist.Tracks.Any(t => t.Id == trackId))
                return BadRequest("Трек уже есть в плейлисте.");

            playlist.Tracks.Add(track);
            await _context.SaveChangesAsync();

            return Ok("Трек добавлен.");
        }

        [HttpPost("playlists/{id}/remove")]
        public async Task<IActionResult> RemoveTrackFromPlaylist(int id, [FromBody] int trackId)
        {
            var playlist = await _context.Playlists
                .Include(p => p.Tracks)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (playlist == null)
                return NotFound("Плейлист не найден.");

            var track = playlist.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track == null)
                return BadRequest("Трека нет в плейлисте.");

            playlist.Tracks.Remove(track);
            await _context.SaveChangesAsync();

            return Ok("Трек удалён.");
        }

        [HttpPut("playlists/{id}/edit")]
        public async Task<IActionResult> EditPlaylist(int id, [FromBody] PlaylistDTO updatedPlaylist)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Не авторизован.");

            var playlist = await _context.Playlists.FindAsync(id);
            if (playlist == null)
                return NotFound("Плейлист не найден.");

            if (playlist.CreatorId != userId)
                return Forbid("Вы не являетесь владельцем этого плейлиста.");

            playlist.Title = updatedPlaylist.Title;
            playlist.Description = updatedPlaylist.Description;
            playlist.CoverUrl = updatedPlaylist.CoverUrl;
            playlist.Type = updatedPlaylist.Type;

            await _context.SaveChangesAsync();

            return Ok("Плейлист успешно обновлён.");
        }

        [HttpPost("playlists/{id}/add_playlist")]
        public async Task<IActionResult> SavePlaylist(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var exists = await _context.UserPlaylists
                .AnyAsync(up => up.UserId == userId && up.PlaylistId == id);

            if (exists)
                return BadRequest("Плейлист уже добавлен.");

            _context.UserPlaylists.Add(new UserPlaylist
            {
                UserId = userId.Value,
                PlaylistId = id,
                IsOwner = false
            });

            await _context.SaveChangesAsync();
            return Ok("Плейлист добавлен в медиатеку.");
        }

        [HttpDelete("playlist/{id}/remove_playlist")]
        public async Task<IActionResult> UnsavePlaylist(int id)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var userPlaylist = await _context.UserPlaylists
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PlaylistId == id && (up.IsOwner == false || up.IsOwner == null));

            if (userPlaylist == null)
                return NotFound("Плейлист не найден в медиатеке.");

            _context.UserPlaylists.Remove(userPlaylist);
            await _context.SaveChangesAsync();

            return Ok("Плейлист удалён из медиатеки.");
        }

        private int? GetCurrentUserId()
        {
            if (!Request.Cookies.TryGetValue("userId", out var userIdStr) ||
                !int.TryParse(userIdStr, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}
