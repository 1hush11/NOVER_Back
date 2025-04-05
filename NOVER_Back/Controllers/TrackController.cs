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
        private readonly NoverDbContext _context;

        public TrackController(NoverDbContext context)
        {
            _context = context;
        }

        [HttpGet("tracks")]
        public async Task<ActionResult<IEnumerable<Track>>> GetTracks()
        {
            var tracks = await _context.Tracks
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .ToListAsync();

            return tracks.Any() ? Ok(tracks) : NotFound("Треки не найдены.");
        }

        [HttpGet("tracks/{id}")]
        public async Task<ActionResult<Track>> GetTrackById([FromRoute] int id)
        {
            var track = await _context.Tracks
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .FirstOrDefaultAsync(t => t.Id == id);

            return track == null ? NotFound($"Трек с id {id} не найден.") : Ok(track);
        }

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<Track>>> GetTopTracks([FromQuery] int count = 10)
        {
            if (count <= 0 || count > 100)
            {
                return BadRequest("Количество треков должно быть от 1 до 100. Не испытывай судьбу.");
            }

            var topTracks = await _context.Tracks
                .OrderByDescending(t => t.PlayCount)
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .Take(count)
                .ToListAsync();

            if (!topTracks.Any())
            {
                return NotFound($"Не найдено ни одного из топ {count} треков.");
            }

            return Ok(topTracks);
        }

        [HttpPost("track")]
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
        public async Task<ActionResult<Track>> GetCurrentTrack()
        {
            if (!Request.Cookies.TryGetValue("currentTrackId", out var trackIdStr) ||
                !int.TryParse(trackIdStr, out var trackId))
            {
                return NotFound("Текущий трек не выбран.");
            }

            var track = await _context.Tracks
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .FirstOrDefaultAsync(t => t.Id == trackId);

            return track == null
                ? NotFound("Текущий трек не найден.")
                : Ok(track);
        }
    }
}
