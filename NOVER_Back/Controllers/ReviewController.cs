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
    }
}
