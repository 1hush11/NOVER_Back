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
                SubscribersCount = s.SubscribersCount
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
    }
}
