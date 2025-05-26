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
        private readonly DbNoverContext _context;

        public AlbumController(DbNoverContext context)
        {
            _context = context;
        }

        [HttpGet("albums")]
        public async Task<ActionResult<IEnumerable<AlbumWithTrackDTO>>> GetAlbums()
        {
            var albums = await _context.Albums
                .OrderBy(t => t.Id)
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
            
            album.Tracks = album.Tracks
                .Where(t => t.Status == "Активен")
                .ToList();

            var result = MapToDTO(album);
            return Ok(result);
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
