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
                .Include(a => a.Singer)
                .Include(a => a.Tracks)
                    .ThenInclude(t => t.Singers)
                .Where(a => a.Singer != null && a.Singer.Status == "Активен")
                .OrderBy(a => a.Id)
                .ToListAsync();

            var result = albums.Select(a => new AlbumWithTrackDTO
            {
                Id = a.Id,
                Name = a.Name,
                SingerId = a.SingerId,
                CoverUrl = a.CoverUrl,
                ReleaseDate = a.ReleaseDate,
                Singer = new SingerDTO
                {
                    Id = a.Singer!.Id,
                    Name = a.Singer.Name,
                    PhotoUrl = a.Singer.PhotoUrl,
                    Description = a.Singer.Description,
                    ViewCount = a.Singer.ViewCount,
                    SubscribersCount = a.Singer.SubscribersCount
                },
                Tracks = a.Tracks
                    .Where(t => t.Status == "Активен" && t.Singers.All(s => s.Status == "Активен"))
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
                        Singers = t.Singers
                            .Where(s => s.Status == "Активен")
                            .Select(s => s.Name)
                            .ToList()
                    }).ToList()
            }).ToList();

            return Ok(result);
        }
        [HttpGet("album/{id}")]
        public async Task<ActionResult<AlbumWithTrackDTO>> GetAlbumById(int id)
        {
            var album = await _context.Albums
                .Include(a => a.Singer)
                .Include(a => a.Tracks)
                    .ThenInclude(t => t.Singers)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (album == null || album.Singer?.Status != "Активен")
                return NotFound($"Альбом с ID {id} не найден или исполнитель неактивен.");

            var result = new AlbumWithTrackDTO
            {
                Id = album.Id,
                Name = album.Name,
                SingerId = album.SingerId,
                CoverUrl = album.CoverUrl,
                ReleaseDate = album.ReleaseDate,
                Singer = new SingerDTO
                {
                    Id = album.Singer.Id,
                    Name = album.Singer.Name,
                    PhotoUrl = album.Singer.PhotoUrl,
                    Description = album.Singer.Description,
                    ViewCount = album.Singer.ViewCount,
                    SubscribersCount = album.Singer.SubscribersCount
                },
                Tracks = album.Tracks
                    .Where(t => t.Status == "Активен" && t.Singers.All(s => s.Status == "Активен"))
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
                        Singers = t.Singers
                            .Where(s => s.Status == "Активен")
                            .Select(s => s.Name)
                            .ToList()
                    }).ToList()
            };

            return Ok(result);
        }
    }
}
