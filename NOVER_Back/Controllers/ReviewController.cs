using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;

namespace NOVER_Back.Controllers
{
    [Route("api/review")]
    [ApiController]
    public class ReviewController : ControllerBase
    {
        private readonly DbNoverContext _context;

        public ReviewController(DbNoverContext context)
        {
            _context = context;
        }

        [HttpGet("reviews")]
        public async Task<IActionResult> GetAllReviews()
        {
            var reviews = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Track)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    TrackId = c.TrackId,
                    TrackName = c.Track.Name,
                    User = c.User.Username,
                    Comment = c.CommentText,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpGet("track/{trackId}")]
        public async Task<IActionResult> GetReviewsByTrack(int trackId)
        {
            var track = await _context.Tracks.FindAsync(trackId);
            if (track == null)
                return NotFound("Трек не найден.");

            var reviews = await _context.Comments
                .Where(c => c.TrackId == trackId)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    User = c.User.Username,
                    Comment = c.CommentText,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        [HttpGet("top")]
        public async Task<IActionResult> GetTopRatedTracks()
        {
            var topTracks = await _context.Tracks
                .Where(t => t.Ratings.Any())
                .Select(t => new
                {
                    TrackId = t.Id,
                    Name = t.Name,
                    AverageRating = t.Ratings.Average(r => r.Rating1),
                    RatingCount = t.Ratings.Count,
                    Genre = t.Genre!.Name,
                    CoverUrl = t.CoverUrl
                })
                .OrderByDescending(t => t.AverageRating)
                .ThenByDescending(t => t.RatingCount)
                .Take(20)
                .ToListAsync();

            return Ok(topTracks);
        }

        [HttpGet("top_by_genre")]
        public async Task<IActionResult> GetTopRatedTracksByGenre()
        {
            var genreGroups = await _context.Genres
                .Include(g => g.Tracks)
                .ThenInclude(t => t.Ratings)
                .ToListAsync();

            var result = genreGroups
                .Select(g => new
                {
                    Genre = g.Name,
                    Tracks = g.Tracks
                        .Where(t => t.Ratings.Any())
                        .Select(t => new
                        {
                            TrackId = t.Id,
                            Name = t.Name,
                            AverageRating = t.Ratings.Average(r => r.Rating1),
                            RatingCount = t.Ratings.Count,
                            CoverUrl = t.CoverUrl
                        })
                        .OrderByDescending(t => t.AverageRating)
                        .ThenByDescending(t => t.RatingCount)
                        .Take(5)
                        .ToList()
                })
                .Where(g => g.Tracks.Any())
                .ToList();

            return Ok(result);
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

        [HttpPost("generate_genre_playlists")]
        public async Task<IActionResult> GenerateGenrePlaylists()
        {
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Администратор");
            if (admin == null)
                return BadRequest("Не найден администратор для создания плейлистов.");

            var genres = await _context.Genres
                .Include(g => g.Tracks)
                    .ThenInclude(t => t.Ratings)
                .Include(g => g.Tracks)
                    .ThenInclude(t => t.Singers)
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
                var existing = await _context.Playlists
                    .FirstOrDefaultAsync(p => p.Title == playlistTitle && p.CreatorId == admin.Id);

                if (existing != null)
                    continue;

                var playlist = new Playlist
                {
                    Title = playlistTitle,
                    CreatorId = admin.Id,
                    Type = "public",
                    CreatedAt = DateTime.Now,
                    Description = $"Лучшие треки в жанре {genre.Name}",
                    CoverUrl = genre.CoverUrl
                };

                foreach (var track in topTracks)
                {
                    playlist.Tracks.Add(track);
                }

                _context.Playlists.Add(playlist);

                _context.UserPlaylists.Add(new UserPlaylist
                {
                    UserId = admin.Id,
                    Playlist = playlist,
                    IsOwner = true
                });

                await _context.SaveChangesAsync();
            }

            return Ok("Плейлисты по жанрам успешно созданы.");
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
