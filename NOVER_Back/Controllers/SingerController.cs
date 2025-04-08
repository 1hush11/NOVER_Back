using Microsoft.AspNetCore.Mvc;
using NOVER_Back.Models.DTOs;
using NOVER_Back.Models;
using Microsoft.EntityFrameworkCore;

namespace NOVER_Back.Controllers
{
    [Route("api/singer")]
    [ApiController]
    public class SingerController : ControllerBase
    {
        private readonly NoverDbContext _context;

        public SingerController(NoverDbContext context)
        {
            _context = context;
        }

        [HttpGet("singers")]
        public async Task<ActionResult<IEnumerable<SingerDTO>>> GetSingers()
        {
            var singers = await _context.Singers.ToListAsync();

            var result = singers.Select(s => new SingerDTO
            {
                Id = s.Id,
                Name = s.Name,
                PhotoUrl = s.PhotoUrl,
                Description = s.Description,
                ViewCount = s.ViewCount,
                SubscribersCount = s.SubscribersCount
            }).ToList();

            return Ok(result);
        }

        [HttpGet("singers/{id}")]
        public async Task<ActionResult<object>> GetSingerWithTracks(int id)
        {
            var singer = await _context.Singers
                .Include(s => s.Tracks)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (singer == null)
                return NotFound($"Исполнитель с ID {id} не найден.");

            var result = new
            {
                Singer = new SingerDTO
                {
                    Id = singer.Id,
                    Name = singer.Name,
                    PhotoUrl = singer.PhotoUrl,
                    Description = singer.Description,
                    ViewCount = singer.ViewCount,
                    SubscribersCount = singer.SubscribersCount
                },
                Tracks = singer.Tracks.Select(t => new TrackDTO
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
                    Status = t.Status
                }).ToList()
            };

            return Ok(result);
        }

        [HttpPost("subscribe/{singerId}")]
        public async Task<IActionResult> SubscribeToSinger(int singerId)
        {
            var userId = GetCurrentUserId();

            if (userId == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Singers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var singer = await _context.Singers.FindAsync(singerId);

            if (user == null || singer == null)
                return NotFound("Пользователь или исполнитель не найден.");

            if (user.Singers.Any(s => s.Id == singerId))
                return BadRequest("Вы уже подписаны на этого исполнителя.");

            user.Singers.Add(singer);
            singer.SubscribersCount = (singer.SubscribersCount ?? 0) + 1;

            await _context.SaveChangesAsync();

            return Ok("Подписка оформлена.");
        }

        [HttpPost("unsubscribe/{singerId}")]
        public async Task<IActionResult> UnsubscribeFromSinger(int singerId)
        {
            var userId = GetCurrentUserId();

            if (userId == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Singers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            var singer = await _context.Singers.FindAsync(singerId);

            if (user == null || singer == null)
                return NotFound("Пользователь или исполнитель не найден.");

            var subscribed = user.Singers.FirstOrDefault(s => s.Id == singerId);
            if (subscribed == null)
                return BadRequest("Вы не подписаны на этого исполнителя.");

            user.Singers.Remove(subscribed);
            singer.SubscribersCount = Math.Max((singer.SubscribersCount ?? 1) - 1, 0);

            await _context.SaveChangesAsync();

            return Ok("Подписка отменена.");
        }

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<SingerDTO>>> GetTopSingers([FromQuery] int count = 10)
        {
            var topSingers = await _context.Singers
                .OrderByDescending(s => s.SubscribersCount)
                .Take(count)
                .ToListAsync();

            var result = topSingers.Select(s => new SingerDTO
            {
                Id = s.Id,
                Name = s.Name,
                PhotoUrl = s.PhotoUrl,
                Description = s.Description,
                ViewCount = s.ViewCount,
                SubscribersCount = s.SubscribersCount,
            }).ToList();

            return Ok(result);
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
        [HttpGet("singers/{id}/top_tracks")]
        public async Task<ActionResult<IEnumerable<TrackDTO>>> GetTopTracksBySinger(int id, [FromQuery] int count = 10)
        {
            var singer = await _context.Singers
                .Include(s => s.Tracks)
                    .ThenInclude(t => t.Singers)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (singer == null)
                return NotFound("Исполнитель не найден.");

            var topTracks = singer.Tracks
                .OrderByDescending(t => t.PlayCount ?? 0)
                .Take(count)
                .Select(t => new TrackDTO
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
                }).ToList();

            return Ok(topTracks);
        }


        [HttpGet("singers/{id}/similar")]
        public async Task<ActionResult<IEnumerable<object>>> GetSimilarSingers(int id)
        {
            var singer = await _context.Singers
                .Include(s => s.Tracks)
                .ThenInclude(t => t.Genre)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (singer == null)
                return NotFound("Исполнитель не найден.");

            var genreIds = singer.Tracks
                .Where(t => t.GenreId != null)
                .Select(t => t.GenreId!.Value)
                .Distinct()
                .ToList();

            var similarSingers = await _context.Singers
                .Where(s => s.Id != id)
                .Where(s => s.Tracks.Any(t => genreIds.Contains(t.GenreId ?? -1)))
                .Include(s => s.Tracks) 
                .Distinct()
                .Take(10)
                .ToListAsync();

            var result = similarSingers.Select(s => new
            {
                Id = s.Id,
                Name = s.Name,
                PhotoUrl = s.PhotoUrl,
                Description = s.Description,
                ViewCount = s.ViewCount,
                SubscribersCount = s.SubscribersCount,
                Tracks = s.Tracks.Select(t => new TrackDTO
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
                }).ToList()
            });

            return Ok(result);
        }

        [HttpGet("singers/{id}/albums")]
        public async Task<ActionResult<IEnumerable<AlbumWithTrackDTO>>> GetSingerAlbums(int id)
        {
            var albums = await _context.Albums
                .Include(a => a.Tracks)
                .Include(a => a.Singer)
                .Where(a => a.SingerId == id)
                .ToListAsync();

            var result = albums.Select(album => new AlbumWithTrackDTO
            {
                Id = album.Id,
                Name = album.Name,
                SingerId = album.SingerId,
                CoverUrl = album.CoverUrl,
                ReleaseDate = album.ReleaseDate,
                Singer = album.Singer == null ? null : new SingerDTO
                {
                    Id = album.Singer.Id,
                    Name = album.Singer.Name,
                    PhotoUrl = album.Singer.PhotoUrl,
                    Description = album.Singer.Description,
                    ViewCount = album.Singer.ViewCount,
                    SubscribersCount = album.Singer.SubscribersCount
                },
                Tracks = album.Tracks.Select(t => new TrackDTO
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
                    Status = t.Status
                }).ToList()
            }).ToList();

            return Ok(result);
        }
    }
}
