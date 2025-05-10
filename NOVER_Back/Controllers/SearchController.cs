using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;

namespace NOVER_Back.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly DbNoverContext _context;

        public SearchController(DbNoverContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest("Поисковый запрос не может быть пустым.");

            var query = q.Trim().ToLower();

            // Поиск по трекам с исполнителями
            var tracks = await _context.Tracks
                .Include(t => t.Singers)
                .Include(t => t.Genre)
                .Include(t => t.Album)
                .Where(t => EF.Functions.ILike(t.Name, $"%{query}%") || t.Singers.Any(s => EF.Functions.ILike(s.Name, $"%{query}%")))
                .Select(t => new
                {
                    Id = t.Id,
                    Title = t.Name,
                    Duration = t.Duration,
                    Genre = t.Genre,
                    Album = t.Album,
                    AudioUrl = t.AudioUrl,
                    CoverUrl = t.CoverUrl,
                    Singers = t.Singers.Select(s => s.Name).ToList(),
                    Type = "track"
                }).ToListAsync();

            // Поиск по исполнителям
            var singers = await _context.Singers
                .Where(s => EF.Functions.ILike(s.Name, $"%{query}%"))
                .Select(s => new
                {
                    Id = s.Id,
                    Name = s.Name,
                    PhotoUrl = s.PhotoUrl,
                    SubscribersCount = s.SubscribersCount,
                    TotalTracks = s.Tracks.Count,
                    TotalPlayCount = s.Tracks.Sum(t => t.PlayCount),
                    Type = "artist"
                }).ToListAsync();

            // Поиск по альбомам
            var albums = await _context.Albums
                .Where(a => EF.Functions.ILike(a.Name, $"%{query}%"))
                .Select(a => new
                {
                    Id = a.Id,
                    Name = a.Name,
                    CoverUrl = a.CoverUrl,
                    ReleaseDate = a.ReleaseDate,
                    Singer = a.Singer,
                    SingerCover = a.Singer!.PhotoUrl,
                    Type = "album"
                }).ToListAsync();

            // Поиск по жанрам
            var genres = await _context.Genres
                .Where(g => EF.Functions.ILike(g.Name, $"%{query}%"))
                .Select(g => new
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CoverUrl = g.CoverUrl,
                    Type = "genre"
                }).ToListAsync();

            // Поиск по плейлистам
            var playlists = await _context.Playlists
                .Include(p => p.UserPlaylists)
                .ThenInclude(up => up.User)
                .Where(p => EF.Functions.ILike(p.Title, $"%{query}%"))
                .Select(p => new
                {
                    Id = p.Id,
                    Title = p.Title,
                    User = p.Creator!.Username,
                    CoverUrl = p.CoverUrl,
                    Type = "playlist"
                }).ToListAsync();

            return Ok(new
            {
                tracks,
                singers,
                albums,
                genres,
                playlists
            });
        }
    }
}
