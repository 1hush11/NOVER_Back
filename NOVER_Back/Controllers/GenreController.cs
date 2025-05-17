using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;
using NOVER_Back.Models.DTOs;

namespace NOVER_Back.Controllers
{
    [Route("api/genre")]
    [ApiController]
    public class GenreController : ControllerBase
    {
        private readonly DbNoverContext _context;

        public GenreController(DbNoverContext context)
        {
            _context = context;
        }

        [HttpGet("genres")]
        public async Task<ActionResult<IEnumerable<Genre>>> GetGenres()
        {
            var genres = await _context.Genres.OrderBy(g => g.Id).ToListAsync();
            return Ok(genres);
        }

        [HttpGet("genres/{id}")]
        public async Task<ActionResult<Genre>> GetGenreById(int id)
        {
            var genre = await _context.Genres.FindAsync(id);

            if (genre == null)
                return NotFound($"Жанр с ID {id} не найден.");

            return Ok(genre);
        }

        [HttpGet("genres/{id}/tracks")]
        public async Task<ActionResult<IEnumerable<TrackDTO>>> GetTracksByGenre(int id, [FromQuery] int count = 5)
        {
            var genre = await _context.Genres
                .Include(g => g.Tracks)
                .ThenInclude(t => t.Singers)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (genre == null)
                return NotFound($"Жанр с ID {id} не найден.");

            var result = genre.Tracks
                .OrderByDescending(t => t.PlayCount)
                .Take(count)
                .Select(t => new TrackDTO
                {
                    Id = t.Id,
                    Name = t.Name,
                    AlbumId = t.AlbumId,
                    AlbumTitle = t.Album != null ? t.Album.Name : null,
                    Duration = t.Duration,
                    GenreId = t.GenreId,
                    GenreName = genre.Name,
                    ReleaseDate = t.ReleaseDate,
                    PlayCount = t.PlayCount,
                    AudioUrl = t.AudioUrl,
                    CoverUrl = t.CoverUrl,
                    Status = t.Status,
                    Singers = t.Singers.Select(s => s.Name).ToList()
                }).ToList();


            return Ok(result);
        }

        [HttpGet("genres/{id}/singers")]
        public async Task<ActionResult<IEnumerable<SingerDTO>>> GetSingersByGenre(int id, [FromQuery] int count = 6)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null)
                return NotFound($"Жанр с ID {id} не найден.");

            var tracks = await _context.Tracks
                .Where(t => t.GenreId == id)
                .Include(t => t.Singers)
                .ToListAsync();

            var singers = tracks
                .SelectMany(t => t.Singers)
                .Distinct()
                .Take(count)
                .ToList();

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
    }
}
