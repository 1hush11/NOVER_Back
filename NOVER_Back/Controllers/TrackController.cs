using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;
using NOVER_Back.Models.DTOs;

namespace NOVER_Back.Controllers
{
    [Route("api/track")]
    [ApiController]
    public class TrackController : ControllerBase
    {
        private readonly DbNoverContext _context;

        public TrackController(DbNoverContext context)
        {
            _context = context;
        }

        [HttpGet("tracks")]
        public async Task<ActionResult<IEnumerable<TrackDTO>>> GetTracks()
        {
            var tracks = await _context.Tracks
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .Include(t => t.Singers)
                .ToListAsync();

            var result = tracks.Select(t => new TrackDTO
            {
                Id = t.Id,
                Name = t.Name,
                AlbumId = t.AlbumId,
                AlbumTitle = t.Album?.Name,
                Duration = t.Duration,
                GenreId = t.GenreId,
                GenreName = t.Genre?.Name,
                ReleaseDate = t.ReleaseDate,
                PlayCount = t.PlayCount,
                AudioUrl = t.AudioUrl,
                CoverUrl = t.CoverUrl,
                Status = t.Status,
                Singers = t.Singers.Select(s => s.Name).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpGet("tracks/{id}")]
        public async Task<ActionResult<TrackDTO>> GetTrackById([FromRoute] int id)
        {
            var track = await _context.Tracks
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .Include(t => t.Singers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (track == null)
                return NotFound($"Трек с id {id} не найден.");

            var dto = new TrackDTO
            {
                Id = track.Id,
                Name = track.Name,
                AlbumId = track.AlbumId,
                AlbumTitle = track.Album?.Name,
                Duration = track.Duration,
                GenreId = track.GenreId,
                GenreName = track.Genre?.Name,
                ReleaseDate = track.ReleaseDate,
                PlayCount = track.PlayCount,
                AudioUrl = track.AudioUrl,
                CoverUrl = track.CoverUrl,
                Status = track.Status,
                Singers = track.Singers.Select(s => s.Name).ToList()
            };

            return Ok(dto);
        }

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<Track>>> GetTopTracks([FromQuery] int count = 10)
        {
            if (count <= 0 || count > 100)
            {
                return BadRequest("Количество треков должно быть от 1 до 100.");
            }

            var topTracks = await _context.Tracks
                .OrderByDescending(t => t.PlayCount)
                .Include(t => t.Singers)
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .Take(count)
                .ToListAsync();

            if (!topTracks.Any())
            {
                return NotFound($"Не найдено ни одного из топ {count} треков.");
            }

            var dto = topTracks.Select(t => new TrackDTO
            {
                Id = t.Id,
                Name = t.Name,
                AlbumId = t.AlbumId,
                AlbumTitle = t.Album?.Name,
                Duration = t.Duration,
                GenreId = t.GenreId,
                GenreName = t.Genre?.Name,
                ReleaseDate = t.ReleaseDate,
                PlayCount = t.PlayCount,
                AudioUrl = t.AudioUrl,
                CoverUrl = t.CoverUrl,
                Status = t.Status,
                Singers = t.Singers.Select(s => s.Name).ToList()
            }).ToList();

            return Ok(dto);

        }

        [HttpPost("add_track")]
        public async Task<ActionResult<Track>> AddTrack([FromBody] TrackDTO track)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (track.AlbumId.HasValue && !await _context.Albums.AnyAsync(a => a.Id == track.AlbumId))
                return BadRequest($"Альбом с ID {track.AlbumId} не найден.");

            if (track.GenreId.HasValue && !await _context.Genres.AnyAsync(g => g.Id == track.GenreId))
                return BadRequest($"Жанр с ID {track.GenreId} не найден.");

            var newTrack = new Track
            {
                Name = track.Name,
                AlbumId = track.AlbumId,
                Duration = track.Duration,
                GenreId = track.GenreId,
                ReleaseDate = track.ReleaseDate,
                PlayCount = track.PlayCount ?? 0,
                AudioUrl = track.AudioUrl,
                CoverUrl = track.CoverUrl,
                Status = track.Status
            };

            await _context.Tracks.AddAsync(newTrack);
            await _context.SaveChangesAsync();

            return Ok(newTrack);
        }

        [HttpPost("set_current/{id}")]
        public async Task<IActionResult> SetCurrentTrack([FromRoute] int id)
        {
            var existingTrack = await _context.Tracks.FirstOrDefaultAsync(t => t.Id == id);
            if (existingTrack != null)
            {
                Response.Cookies.Append("currentTrackId", id.ToString(), new CookieOptions
                {
                    HttpOnly = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTimeOffset.UtcNow.AddHours(2)
                });

                return Ok($"Текущий трек установлен: {id}");
            }
            else { return NotFound($"Трек с id {id} не найден."); }
        }

        [HttpGet("current")]
        public async Task<ActionResult<TrackDTO>> GetCurrentTrack()
        {
            if (!Request.Cookies.TryGetValue("currentTrackId", out var trackIdStr) ||
                !int.TryParse(trackIdStr, out var trackId))
            {
                return NotFound("Текущий трек не выбран.");
            }

            var track = await _context.Tracks
                .Include(t => t.Singers)
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .FirstOrDefaultAsync(t => t.Id == trackId);

            if (track == null)
                return NotFound("Трек не найден.");

            var dto = new TrackDTO
            {
                Id = track.Id,
                Name = track.Name,
                GenreName = track.Genre?.Name,
                AlbumTitle = track.Album?.Name,
                Singers = track.Singers.Select(s => s.Name).ToList(),
                CoverUrl = track.CoverUrl,
                AudioUrl = track.AudioUrl,
                Duration = track.Duration
            };

            return Ok(dto);
        }


        [HttpPost("{id}/play")]
        public async Task<IActionResult> IncrementPlayCount(int id)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track == null)
                return NotFound("Трек не найден.");

            track.PlayCount = (track.PlayCount ?? 0) + 1;
            await _context.SaveChangesAsync();

            return Ok(new { track.Id, track.PlayCount });
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteTrack(int id)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track == null)
                return NotFound("Трек не найден.");

            _context.Tracks.Remove(track);
            await _context.SaveChangesAsync();

            return Ok("Трек удалён.");
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateTrack(int id, [FromBody] TrackDTO dto)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track == null)
                return NotFound("Трек не найден.");

            track.Name = dto.Name;
            track.AlbumId = dto.AlbumId;
            track.Duration = dto.Duration;
            track.GenreId = dto.GenreId;
            track.ReleaseDate = dto.ReleaseDate;
            track.PlayCount = dto.PlayCount;
            track.AudioUrl = dto.AudioUrl;
            track.CoverUrl = dto.CoverUrl;
            track.Status = dto.Status;

            await _context.SaveChangesAsync();
            return Ok("Трек обновлён.");
        }

        [HttpGet("by_singer/{singerId}")]
        public async Task<ActionResult<IEnumerable<TrackDTO>>> GetTracksBySinger(int singerId)
        {
            var singer = await _context.Singers
                .Include(s => s.Tracks)
                    .ThenInclude(t => t.Album)
                .Include(s => s.Tracks)
                    .ThenInclude(t => t.Genre)
                .Include(s => s.Tracks)
                    .ThenInclude(t => t.Singers)
                .FirstOrDefaultAsync(s => s.Id == singerId);

            if (singer == null)
                return NotFound("Исполнитель не найден.");

            var result = singer.Tracks.Select(t => new TrackDTO
            {
                Id = t.Id,
                Name = t.Name,
                AlbumId = t.AlbumId,
                AlbumTitle = t.Album?.Name,
                Duration = t.Duration,
                GenreId = t.GenreId,
                GenreName = t.Genre?.Name,
                ReleaseDate = t.ReleaseDate,
                PlayCount = t.PlayCount,
                AudioUrl = t.AudioUrl,
                CoverUrl = t.CoverUrl,
                Status = t.Status,
                Singers = t.Singers.Select(s => s.Name).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpGet("similar/{id}")]
        public async Task<ActionResult<IEnumerable<TrackDTO>>> GetSimilarTracks([FromRoute] int id, [FromQuery] int count = 5)
        {
            if (count <= 0 || count > 100)
            {
                return BadRequest("Количество треков должно быть от 1 до 100.");
            }

            var track = await _context.Tracks
                .Include(t => t.Genre)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (track == null)
            {
                return NotFound($"Трек с id {id} не найден.");
            }

            var similarTracks = await _context.Tracks
                .Where(t => t.GenreId == track.GenreId && t.Id != id)
                .Include(t => t.Genre)
                .Include(t => t.Singers)
                .Take(count)
                .ToListAsync();

            var result = similarTracks.Select(t => new TrackDTO
            {
                Id = t.Id,
                Name = t.Name,
                AlbumId = t.AlbumId,
                AlbumTitle = t.Album?.Name,
                Duration = t.Duration,
                GenreId = t.GenreId,
                GenreName = t.Genre?.Name,
                ReleaseDate = t.ReleaseDate,
                PlayCount = t.PlayCount,
                AudioUrl = t.AudioUrl,
                CoverUrl = t.CoverUrl,
                Status = t.Status,
                Singers = t.Singers.Select(s => s.Name).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<TrackDTO>>> SearchTracks([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Пустой запрос.");

            var tracks = await _context.Tracks
                .Where(t => EF.Functions.ILike(t.Name, $"%{query}%"))
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .Include(t => t.Singers)
                .ToListAsync();

            var result = tracks.Select(t => new TrackDTO
            {
                Id = t.Id,
                Name = t.Name,
                AlbumId = t.AlbumId,
                AlbumTitle = t.Album?.Name,
                Duration = t.Duration,
                GenreId = t.GenreId,
                GenreName = t.Genre?.Name,
                ReleaseDate = t.ReleaseDate,
                PlayCount = t.PlayCount,
                AudioUrl = t.AudioUrl,
                CoverUrl = t.CoverUrl,
                Status = t.Status,
                Singers = t.Singers.Select(s => s.Name).ToList()
            }).ToList();

            return Ok(result);
        }
    }
}
