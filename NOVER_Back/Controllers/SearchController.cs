using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;
using NOVER_Back.Models.DTOs;

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
                .Where(t => EF.Functions.ILike(t.Name, $"%{query}%")
                         || t.Singers.Any(s => EF.Functions.ILike(s.Name, $"%{query}%")))
                .Select(t => new
                {
                    Id = t.Id,
                    Name = t.Name,
                    Duration = t.Duration,
                    Genre = t.Genre,
                    Album = t.Album,
                    AlbumName = t.Album!.Name,
                    AudioUrl = t.AudioUrl,
                    CoverUrl = t.CoverUrl,
                    Singers = t.Singers
                        .Where(s => s.Status == "Активен")
                        .Select(s => new SingerDTO
                        {
                            Id = s.Id,
                            Name = s.Name
                        })
                        .ToList(),
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
                    Singer = a.Singer == null ? null : new SingerDTO
                    {
                        Id = a.Singer.Id,
                        Name = a.Singer.Name,
                        PhotoUrl = a.Singer.PhotoUrl,
                        Description = a.Singer.Description,
                        ViewCount = a.Singer.ViewCount,
                        SubscribersCount = a.Singer.SubscribersCount
                    },
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

            // Поиск по пользователям (логин и имя)
            var users = await _context.Users
                .Where(u => EF.Functions.ILike(u.Username, $"%{query}%")
                         || EF.Functions.ILike(u.Login, $"%{query}%"))
                .Select(u => new
                {
                    Id = u.Id,
                    Username = u.Username,
                    Login = u.Login,
                    Role = u.Role,
                    Type = "user"
                }).ToListAsync();

            return Ok(new
            {
                tracks,
                singers,
                albums,
                genres,
                playlists,
                users
            });
        }
    }
}
