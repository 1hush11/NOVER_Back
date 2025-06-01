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
        private readonly IEmailService _emailService;

        public AdminController(DbNoverContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
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

        [HttpPut("user/block/{id}")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound("Комментарий не найден.");

            user.Status = "Заблокирован";

            await _context.SaveChangesAsync();

            if (!string.IsNullOrWhiteSpace(user.Login))
            {
                var subject = "Ваш аккаунт был заблокирован";
                var body = $@"
                <p>Здравствуйте, {user.Username}!</p>
                <p>Ваш аккаунт на платформе NOVER был заблокирован администратором.</p>
                <p>Если вы считаете, что это ошибка, пожалуйста, свяжитесь с поддержкой.</p>
                <p>С уважением,<br/>Команда NOVER</p>";
                await _emailService.SendEmailAsync(user.Login, subject, body);
            }

            return Ok("Пользователь заблокирован.");
        }

        [HttpPut("user/unblock/{id}")]
        public async Task<IActionResult> UnblockUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null) return NotFound("Комментарий не найден.");

            user.Status = "Активен";

            await _context.SaveChangesAsync();
            return Ok("Пользователя разблокировали.");
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

        [HttpPut("comment/block/{id}")]
        public async Task<IActionResult> BlockComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            
            if (comment == null) return NotFound("Комментарий не найден.");

            comment.Status = "Заблокирован";

            await _context.SaveChangesAsync();
            return Ok("Комментарий удалён.");
        }
        [HttpPut("comment/unblock/{id}")]
        public async Task<IActionResult> UblockComment(int id)
        {
            var comment = await _context.Comments.FindAsync(id);

            if (comment == null) return NotFound("Комментарий не найден.");

            comment.Status = "Активен";

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
            var existingGenre = await _context.Genres.FindAsync(id);
            
            if (existingGenre == null) return NotFound("Жанр не найден.");

            existingGenre.Name = genre.Name;
            existingGenre.Description = genre.Description;
            existingGenre.CoverUrl = genre.CoverUrl;
            
            await _context.SaveChangesAsync();
            return Ok("Жанр обновлён.");
        }

        [HttpPut("genre/block/{id}")]
        public async Task<IActionResult> BlockGenre(int id)
        {
            var existingGenre = await _context.Genres.FindAsync(id);
            
            if (existingGenre == null) return NotFound("Жанр не найден.");

            existingGenre.Status = "Заблокирован";

            await _context.SaveChangesAsync();
            return Ok("Жанр удалён.");
        }

        [HttpGet("complaints")]
        public async Task<IActionResult> GetComplaints()
        {
            var complaints = await _context.Complaints
                .OrderByDescending(c => c.CreatedAt)
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
                    Singers = c.Track.Singers
                        .Select(s => new {
                            Id = s.Id,
                            Status = s.Status
                        })
                        .ToList(),
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

        [HttpPut("complaint/block/{id}")]
        public async Task<IActionResult> BlockComplaint(int id)
        {
            var complaint = await _context.Complaints.FindAsync(id);

            if (complaint == null) return NotFound("Жалоба не найдена.");

            complaint.Status = "Отклонена";
            await _context.SaveChangesAsync();
            return Ok("Жалоба заблокирована.");
        }
        [HttpPut("complaint/unblock/{id}")]
        public async Task<IActionResult> UnblockComplaint(int id)
        {
            var complaint = await _context.Complaints.FindAsync(id);

            if (complaint == null) return NotFound("Жалоба не найдена.");

            complaint.Status = "На рассмотрении";
            await _context.SaveChangesAsync();
            return Ok("Жалоба заблокирована.");
        }
        [HttpPut("complaint/approve/{id}")]
        public async Task<IActionResult> ApproveComplaint(int id)
        {
            var complaint = await _context.Complaints.FindAsync(id);

            if (complaint == null) return NotFound("Жалоба не найдена.");

            complaint.Status = "Рассмотрено";
            await _context.SaveChangesAsync();
            return Ok("Жалоба заблокирована.");
        }

        [HttpGet("blocked_feedback")]
        public async Task<IActionResult> GetBlockedFeedback()
        {
            var feedback = await _context.Ratings
                .OrderBy(r => r.UserId)
                .Include(r => r.User)
                .Include(r => r.Track)
                    .ThenInclude(t => t.Singers)
                .Join(
                    _context.Comments.Where(c => c.Status == "Заблокирован"),
                    r => new { r.TrackId, r.UserId },
                    c => new { c.TrackId, c.UserId },
                    (r, c) => new
                    {
                        CommentId = c.Id,
                        TrackId = r.TrackId,
                        TrackName = r.Track.Name,
                        Singers = r.Track.Singers.Select(s => s.Name).ToList(),
                        UserId = r.UserId,
                        UserName = r.User.Username,
                        UserLogin = r.User.Login,
                        UserStatus = r.User.Status,
                        RatingValue = r.Rating1,
                        CommentText = c.CommentText,
                        CommentCreatedAt = c.CreatedAt,
                        TotalBlockedCommentsByUser = _context.Comments
                                              .Count(cc => cc.UserId == r.UserId
                                                         && cc.Status == "Заблокирован")
                    }
                )
                .OrderByDescending(r => r.CommentCreatedAt)
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
