using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;
using NOVER_Back.Models.DTOs;

namespace NOVER_Back.Controllers
{
    [Route("api/admin")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly DbNoverContext _context;

        public AdminController(DbNoverContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _context.Users
                .OrderBy(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.Login,
                    AvatarUrl = u.Avatar,
                    Role = u.Role,
                    Status = u.Status 
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpPost("user/block/{id}")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Пользователь не найден.");

            user.Status = "Заблокирован";
            await _context.SaveChangesAsync();
            return Ok("Пользователь заблокирован.");
        }

        [HttpPost("user/unblock/{id}")]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("Пользователь не найден.");

            user.Status = "Активен";
            await _context.SaveChangesAsync();
            return Ok("Пользователь разблокирован.");
        }

        [HttpGet("tracks/pending")]
        public async Task<IActionResult> GetPendingTracks()
        {
            var pendingTracks = await _context.Tracks
                .OrderBy(t => t.Id)
                .Where(t => t.Status == "На модерации")
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .Include(t => t.Singers)
                .Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.AudioUrl,
                    t.CoverUrl,
                    Singers = t.Singers.Select(s => s.Name).ToList()
                })
                .ToListAsync();

            return Ok(pendingTracks);
        }

        [HttpPost("track/approve/{id}")]
        public async Task<IActionResult> ApproveTrack(int id)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track == null)
                return NotFound("Трек не найден");

            track.Status = "Активен";
            await _context.SaveChangesAsync();

            return Ok("Трек одобрен");
        }

        [HttpPost("track/reject/{id}")]
        public async Task<IActionResult> RejectTrack(int id)
        {
            var track = await _context.Tracks.FindAsync(id);
            if (track == null)
                return NotFound("Трек не найден");

            track.Status = "Заблокирован";
            await _context.SaveChangesAsync();

            return Ok("Трек отклонён и заблокирован.");
        }

        [HttpGet("tracks")]
        public async Task<IActionResult> GetAllTrackMetadata()
        {
            var tracks = await _context.Tracks
                .OrderBy(t => t.Id)
                .Include(t => t.Album)
                .Include(t => t.Genre)
                .Include(t => t.Singers)
                .Select(t => new
                {
                    t.Id,
                    Name = t.Name,
                    Singers = t.Singers.Select(s => s.Name).ToList(),
                    AlbumId = t.AlbumId,
                    AlbumName = t.Album != null ? t.Album.Name : null,
                    GenreId = t.GenreId,
                    GenreName = t.Genre != null ? t.Genre.Name : null,
                    ReleaseDate = t.ReleaseDate,
                    Duration = $"{t.Duration / 60:D2}:{t.Duration % 60:D2}",
                    PlayCount = t.PlayCount,
                    AudioUrl = t.AudioUrl,
                    CoverUrl = t.CoverUrl,
                    Status = t.Status
                })
                .ToListAsync();

            return Ok(tracks);
        }

        [HttpPut("track/update/{id}")]
        public async Task<IActionResult> UpdateTrackMetadata(int id, [FromBody] TrackDTO dto)
        {
            var track = await _context.Tracks
                .Include(t => t.Singers)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (track == null)
                return NotFound("Трек не найден.");

            track.Name = dto.Name;
            track.ReleaseDate = dto.ReleaseDate;
            track.Duration = dto.Duration;

            if (!string.IsNullOrWhiteSpace(dto.AlbumTitle))
            {
                var album = await _context.Albums.FirstOrDefaultAsync(a => a.Name == dto.AlbumTitle);
                if (album != null) track.AlbumId = album.Id;
            }

            if (!string.IsNullOrWhiteSpace(dto.GenreName))
            {
                var genre = await _context.Genres.FirstOrDefaultAsync(g => g.Name == dto.GenreName);
                if (genre != null) track.GenreId = genre.Id;
            }

            track.Singers.Clear();
            foreach (var singerName in dto.Singers.Distinct())
            {
                var singer = await _context.Singers
                    .FirstOrDefaultAsync(s => s.Name == singerName)
                    ?? new Singer { Name = singerName };
                track.Singers.Add(singer);
            }

            await _context.SaveChangesAsync();
            return Ok("Метаданные трека обновлены.");
        }

        [HttpDelete("comment/{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            
            if (comment == null) return NotFound("Комментарий не найден.");
            
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return Ok("Комментарий удалён.");
        }

        [HttpPost("genre/add")]
        public async Task<IActionResult> AddGenre([FromBody] Genre genre)
        {
            if (!await _context.Genres.AnyAsync(g => g.Name == genre.Name))
            {
                genre.Id = 0;
                _context.Genres.Add(genre);
                await _context.SaveChangesAsync();
                return Ok("Жанр добавлен.");
            }
            else
            {
                return Conflict("Жанр с таким названием уже существует.");
            }
        }

        [HttpPut("genre/update/{id}")]
        public async Task<IActionResult> UpdateGenre(int id, [FromBody] Genre genre)
        {
            var existing = await _context.Genres.FindAsync(id);
            
            if (existing == null) return NotFound("Жанр не найден.");
            
            existing.Name = genre.Name;
            existing.Description = genre.Description;
            existing.CoverUrl = genre.CoverUrl;
            
            await _context.SaveChangesAsync();
            return Ok("Жанр обновлён.");
        }

        [HttpDelete("genre/delete/{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
           
            if (genre == null) return NotFound("Жанр не найден.");
            
            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();
            return Ok("Жанр удалён.");
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> GetComplaints()
        {
            var complaints = await _context.Complaints.Include(c => c.User)
            .Include(c => c.Track)
            .Select(c => new
            {
                Id = c.Id,
                UserId = c.UserId,
                UserName = c.User != null ? c.User.Username : null,
                TrackId = c.TrackId,
                TrackName = c.Track != null ? c.Track.Name : null,
                Content = c.Content,
                CreatedAt = c.CreatedAt
            }).ToListAsync();
            return Ok(complaints);
        }

        [HttpDelete("complaint/{id}")]
        public async Task<IActionResult> DeleteComplaint(int id)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            
            if (complaint == null) return NotFound("Жалоба не найдена.");
            
            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();
            return Ok("Жалоба удалена.");
        }
        [HttpGet("playlists_by_genre")]
        public async Task<IActionResult> GetPlaylistsByGenre()
        {
            var playlists = await _context.Playlists.Where(p => p.Title.StartsWith("Лучшее:") & p.Creator!.Role == "Администратор")
                                                    .Include(p => p.Tracks)
                                                        .ThenInclude(t => t.Singers)
                                                    .Include(p => p.Tracks)
                                                        .ThenInclude(t => t.Genre)
                                                    .OrderBy(p => p.Title)
                                                    .ToListAsync();

            var result = playlists.Select(p => new
            {
                Id = p.Id,
                Title = p.Title,
                CoverUrl = p.CoverUrl,
                Creator = p.Creator?.Username,
                CreatorRole = p.Creator?.Role,
                Tracks = p.Tracks.Select(t => new
                {
                    Id = t.Id,
                    Name = t.Name,
                    CoverUrl = t.CoverUrl,
                    AudioUrl = t.AudioUrl,
                    Singers = t.Singers.Select(s => s.Name).ToList()
                }).ToList()
            }).ToList();

            return Ok(result);
        }

        [HttpPost("update_genre_playlists")]
        public async Task<IActionResult> UpdateGenrePlaylists()
        {
            var genres = await _context.Genres.Include(g => g.Tracks)
                                                .ThenInclude(t => t.Ratings)
                                                .ToListAsync();

            foreach (var genre in genres)
            {
                var topTracks = genre.Tracks
                    .Where(t => t.Ratings.Any())
                    .OrderByDescending(t => t.Ratings.Average(r => r.Rating1))
                    .ThenByDescending(t => t.Ratings.Count)
                    .Take(10)
                    .ToList();

                if (!topTracks.Any()) continue;

                var playlistTitle = $"Лучшее: {genre.Name}";
                var playlist = await _context.Playlists
                    .Include(p => p.Tracks)
                    .FirstOrDefaultAsync(p => p.Title == playlistTitle);

                if (playlist == null)
                {
                    playlist = new Playlist
                    {
                        Title = playlistTitle,
                        Type = "public",
                        CreatedAt = DateTime.Now,
                        Description = $"Лучшие треки в жанре {genre.Name}",
                        CoverUrl = genre.CoverUrl
                    };

                    _context.Playlists.Add(playlist);
                    await _context.SaveChangesAsync();
                }

                playlist.Tracks.Clear();

                foreach (var track in topTracks)
                {
                    playlist.Tracks.Add(track);
                }

                await _context.SaveChangesAsync();
            }

            return Ok("Жанровые плейлисты обновлены.");
        }
    }
}
