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

        [HttpPost("add_genre")]
        public async Task<ActionResult<Genre>> AddGenre([FromBody] Genre genre)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGenreById), new { id = genre.Id }, genre);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateGenre(int id, [FromBody] Genre updatedGenre)
        {
            if (id != updatedGenre.Id)
                return BadRequest("ID жанра не совпадает.");

            var existing = await _context.Genres.FindAsync(id);
            if (existing == null)
                return NotFound($"Жанр с ID {id} не найден.");

            existing.Name = updatedGenre.Name;
            existing.Description = updatedGenre.Description;
            existing.CoverUrl = updatedGenre.CoverUrl;

            await _context.SaveChangesAsync();

            return Ok("Жанр обновлён.");
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);

            if (genre == null)
                return NotFound($"Жанр с ID {id} не найден.");

            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();

            return Ok("Жанр удалён.");
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
