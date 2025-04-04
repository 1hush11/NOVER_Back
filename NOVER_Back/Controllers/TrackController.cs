using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;
using NOVER_Back.Models.DTOs;

namespace NOVER_Back.Controllers
{
    [Route("API/[controller]")]
    [ApiController]
    public class TrackController : ControllerBase
    {
        private NoverDbContext _context;
        public TrackController(NoverDbContext context)
        {
            _context = context;
        }
        private Track? _currentTrack { get; set; }

        [HttpGet("Tracks")]
        public ActionResult<IEnumerable<Track>> GetTracks()
        {
            List<Track> tracks = _context.Tracks.
                Include(t => t.Album).
                Include(t => t.Genre).ToList();
            if (!tracks.Any())
                return NotFound("Треки не найдены.");
            return Ok(tracks);
        }
        [HttpGet("Tracks/id")]
        public ActionResult<IEnumerable<Track>> GetTrackById(int id)
        {
            try
            {
                Track? track = _context.Tracks.
                    Include(t => t.Album).
                    Include(t => t.Genre).FirstOrDefault(t => t.Id == id);
                if (track == null)
                {
                    return NotFound($"Трек с id {id} не найден.");
                }
                return Ok(track);
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, $"Ошибка базы данных: {dbEx.Message}.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}.");
            }
        }
        [HttpGet("TopTracks")]
        public ActionResult<IEnumerable<Track>> GetTopTracks()
        {
            int limit = 20;
            List<Track> topTracks = _context.Tracks.OrderByDescending(t => t.PlayCount)
                .Take(limit)
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .ToList();
            if (!topTracks.Any())
            {
                return NotFound($"Топ {20} найти не получилось.");
            }
            return Ok(topTracks);
        }
        [HttpPost("Track")]
        public ActionResult<IEnumerable<Track>> AddTrack([FromBody] TrackDTO track)
        {
            try
            {
                if (track.AlbumId.HasValue && !_context.Albums.Any(a => a.Id == track.AlbumId))
                {
                    return BadRequest($"Альбом с ID {track.AlbumId} не найден.");
                }

                if (track.GenreId.HasValue && !_context.Genres.Any(g => g.Id == track.GenreId))
                {
                    return BadRequest($"Жанр с ID {track.GenreId} не найден.");
                }

                Track newTrack = new Track
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

                _context.Tracks.Add(newTrack);
                _context.SaveChanges();

                return Ok(newTrack);
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, $"Ошибка базы данных: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }
    }
}
