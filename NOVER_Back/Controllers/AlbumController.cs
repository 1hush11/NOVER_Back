using Microsoft.AspNetCore.Mvc;
using NOVER_Back.Models;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models.DTOs;


namespace NOVER_Back.Controllers
{
    [Route("api/album")]
    [ApiController]
    public class AlbumController : ControllerBase
    {
        private readonly NoverDbContext _context;

        public AlbumController(NoverDbContext context)
        {
            _context = context;
        }

        [HttpGet("albums")]
        public async Task<ActionResult<IEnumerable<AlbumWithTrackDTO>>> GetAlbums()
        {
            var albums = await _context.Albums
                .Include(a => a.Singer)
                .Include(a => a.Tracks)
                .ToListAsync();

            var result = albums.Select(a => MapToDTO(a)).ToList();

            return Ok(result);
        }

        [HttpGet("album/{id}")]
        public async Task<ActionResult<AlbumWithTrackDTO>> GetAlbumById(int id)
        {
            var album = await _context.Albums
                .Include(a => a.Singer)
                .Include(a => a.Tracks)
                .ThenInclude(a => a.Singers)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null)
                return NotFound($"Альбом с ID {id} не найден.");

            var result = MapToDTO(album);
            return Ok(result);
        }

        [HttpPost("add_album")]
        public async Task<ActionResult<AlbumWithTrackDTO>> AddAlbum([FromBody] AlbumWithTrackDTO albumDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (albumDto.SingerId.HasValue && !await _context.Singers.AnyAsync(s => s.Id == albumDto.SingerId))
                return BadRequest($"Исполнитель с ID {albumDto.SingerId} не найден.");

            var album = new Album
            {
                Name = albumDto.Name,
                SingerId = albumDto.SingerId,
                CoverUrl = albumDto.CoverUrl,
                ReleaseDate = albumDto.ReleaseDate,
                Tracks = albumDto.Tracks.Select(t => new Track
                {
                    Name = t.Name,
                    AlbumId = albumDto.Id, 
                    Duration = t.Duration,
                    GenreId = t.GenreId,
                    ReleaseDate = t.ReleaseDate,
                    PlayCount = t.PlayCount,
                    AudioUrl = t.AudioUrl,
                    CoverUrl = t.CoverUrl,
                    Status = t.Status
                }).ToList()
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            var createdAlbum = await _context.Albums
                .Include(a => a.Singer)
                .Include(a => a.Tracks)
                .FirstOrDefaultAsync(a => a.Id == album.Id);

            var result = MapToDTO(createdAlbum!);
            return CreatedAtAction(nameof(GetAlbumById), new { id = result.Id }, result);
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlbum(int id)
        {
            var album = await _context.Albums.FindAsync(id);

            if (album == null)
                return NotFound($"Альбом с ID {id} не найден.");

            _context.Albums.Remove(album);
            await _context.SaveChangesAsync();

            return Ok($"Альбом с ID {id} удалён.");
        }
        private AlbumWithTrackDTO MapToDTO(Album album)
        {
            return new AlbumWithTrackDTO
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
                    Status = t.Status,
                    Singers = t.Singers.Select(s => s.Name).ToList()
                }).ToList()
            };
        }
    }
}
