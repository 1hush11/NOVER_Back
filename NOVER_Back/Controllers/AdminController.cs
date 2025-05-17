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

        [HttpPost("singer/block/{id}")]
        public async Task<IActionResult> BlockSinger(int id)
        {
            var singer = await _context.Singers.FindAsync(id);
            if (singer == null) return NotFound("Исполнитель не найден.");

            singer.Status = "Заблокирован";
            var tracks = await _context.Tracks.Where(t => t.Singers.Any(s => s.Id == id)).ToListAsync();
            foreach (var t in tracks) 
            { 
                t.Status = "Заблокирован"; 
            }
            await _context.SaveChangesAsync();

            return Ok("Исполнитель и его треки заблокированы.");
        }

        [HttpPost("singer/unblock/{id}")]
        public async Task<IActionResult> UnblockSinger(int id)
        {
            var singer = await _context.Singers.FindAsync(id);
            if (singer == null) return NotFound("Исполнитель не найден.");

            singer.Status = "Активен";
            var tracks = await _context.Tracks.Where(t => t.Singers.Any(s => s.Id == id)).ToListAsync();
            foreach (var t in tracks)
            {
                t.Status = "Активен";
            }
            await _context.SaveChangesAsync();

            return Ok("Исполнитель и его треки разблокированы.");
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
            var complaints = await _context.Complaints
                .Include(c => c.User)
                .Include(c => c.Track)
                    .ThenInclude(t => t!.Singers)
                .Select(c => new
                {
                    Id = c.Id,
                    UserId = c.UserId,
                    UserName = c.User!.Username,
                    TrackId = c.TrackId,
                    TrackName = c.Track!.Name,
                    TrackStatus = c.Track.Status,
                    // для кнопок блокировки исполнителя
                    Singers = c.Track.Singers
                        .Select(s => new {
                            Id = s.Id,
                            Status = s.Status
                        })
                        .ToList(),
                    // чтобы на фронте выводить списком имена
                    SingerNames = c.Track.Singers
                        .Select(s => s.Name)
                        .ToList(),
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .OrderBy(c => c.Id)
                .ToListAsync();

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

        [HttpGet("feedback")]
        public async Task<IActionResult> GetAllFeedback()
        {
            var feedback = await _context.Ratings
                .Include(r => r.User)
                .Include(r => r.Track)
                    .ThenInclude(t => t.Singers)
                .GroupJoin(
                    _context.Comments,
                    r => new { r.TrackId, r.UserId },
                    c => new { c.TrackId, c.UserId },
                    (r, cs) => new { Rating = r, Comments = cs }
                )
                .SelectMany(
                    rc => rc.Comments.DefaultIfEmpty(),
                    (rc, c) => new
                    {
                        Id = c != null ? c.Id: 0,
                        rc.Rating.TrackId,
                        TrackName = rc.Rating.Track.Name,
                        Singers = rc.Rating.Track.Singers.Select(s => s.Name).ToList(),
                        rc.Rating.UserId,
                        UserName = rc.Rating.User.Username,
                        Rating = rc.Rating.Rating1,
                        CommentId = c != null ? c.Id : (int?)null,
                        CommentText = c != null ? c.CommentText : null,
                        CommentCreatedAt = c != null ? c.CreatedAt : (DateTime?)null
                    }
                )
                .OrderBy(f => f.TrackId)
                .ThenBy(f => f.UserId)
                .ToListAsync();

            return Ok(feedback);
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
