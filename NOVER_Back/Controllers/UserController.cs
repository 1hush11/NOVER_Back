using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;
using NOVER_Back.Models.DTOs;

namespace NOVER_Back.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly NoverDbContext _context;

        public UserController(NoverDbContext context)
        {
            _context = context;
        }
        [HttpPost("login")]
        public async Task<ActionResult<User>> LogIn([FromBody] LoginDTO credentials)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Login == credentials.Login);

            if (user == null)
                return Unauthorized("Неверный логин или пароль.");

            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, credentials.Password);

            if (result == PasswordVerificationResult.Failed)
                return Unauthorized("Неверный логин или пароль.");

            Response.Cookies.Append("userId", user.Id.ToString(), new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(user);
        }


        [HttpGet("me")]
        public async Task<ActionResult<User>> GetCurrentUser()
        {
            if (!Request.Cookies.TryGetValue("userId", out var userIdStr) ||
                !int.TryParse(userIdStr, out var userId))
            {
                return Unauthorized("Пользователь не авторизован.");
            }

            var user = await _context.Users.FindAsync(userId);

            return user == null
                ? NotFound("Пользователь не найден.")
                : Ok(user);
        }

        [HttpPost("logout")]
        public IActionResult LogOut()
        {
            if (Request.Cookies.ContainsKey("userId"))
            {
                Response.Cookies.Delete("userId");
            }

            return Ok("Пользователь успешно вышел из системы.");
        }

        [HttpPost("signup")]
        public async Task<ActionResult<User>> SignUp([FromBody] UserDTO user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _context.Users.AnyAsync(u => (u.Login == user.Login || u.Username == user.Username) || (u.Login == user.Login && u.Username == user.Username));
            if (exists)
                return Conflict($"Пользователь с таким именем или логином уже существует.");

            var passwordHasher = new PasswordHasher<User>();
            var registrationDate = DateTime.UtcNow.ToLocalTime();

            var newUser = new User
            {
                Username = user.Username,
                Login = user.Login,
                Avatar = user.Avatar,
                RegistrationDate = registrationDate,
                Role = "Пользователь",
                Status = "Активен"
            };

            newUser.PasswordHash = passwordHasher.HashPassword(newUser, user.PasswordHash);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return Ok(newUser);
        }

        [HttpGet("library/tracks")]
        public async Task<ActionResult<IEnumerable<TrackDTO>>> GetUserLibraryTracks()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Tracks)
                    .ThenInclude(t => t.Singers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            var trackDTOs = user.Tracks.Select(t => new TrackDTO
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
            }).ToList();

            return Ok(trackDTOs);
        }
        [HttpPost("library/add_track/{trackId}")]
        public async Task<IActionResult> AddTrackToLibrary(int trackId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Tracks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            var track = await _context.Tracks.FindAsync(trackId);
            if (track == null)
                return NotFound("Трек не найден.");

            if (user.Tracks.Any(t => t.Id == trackId))
                return BadRequest("Трек уже есть в медиатеке пользователя.");

            user.Tracks.Add(track);
            await _context.SaveChangesAsync();

            return Ok("Трек добавлен в медиатеку.");
        }

        [HttpGet("library/playlists")]
        public async Task<ActionResult<object>> GetUserPlaylists()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var createdPlaylists = await _context.UserPlaylists
                .Where(up => up.UserId == userId && up.IsOwner == true)
                .Include(up => up.Playlist)
                    .ThenInclude(p => p.Creator)
                .ToListAsync();

            var savedPlaylists = await _context.UserPlaylists
                .Where(up => up.UserId == userId && (up.IsOwner == false || up.IsOwner == null))
                .Include(up => up.Playlist)
                    .ThenInclude(p => p.Creator)
                .ToListAsync();

            var result = new
            {
                Created = createdPlaylists.Select(up => new
                {
                    up.Playlist.Id,
                    up.Playlist.Title,
                    up.Playlist.CoverUrl,
                    up.Playlist.Description,
                    up.Playlist.CreatedAt,
                    up.Playlist.Type,
                    Creator = up.Playlist.Creator?.Username ?? "Неизвестно",
                    IsOwner = true
                }),
                Saved = savedPlaylists.Select(up => new
                {
                    up.Playlist.Id,
                    up.Playlist.Title,
                    up.Playlist.CoverUrl,
                    up.Playlist.Description,
                    up.Playlist.CreatedAt,
                    up.Playlist.Type,
                    Creator = up.Playlist.Creator?.Username ?? "Неизвестно",
                    IsOwner = false
                })
            };

            return Ok(result);
        }
        [HttpPost("library/add_album/{albumId}")]
        public async Task<IActionResult> AddAlbumToLibrary(int albumId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Tracks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            var album = await _context.Albums
                .Include(a => a.Tracks)
                .FirstOrDefaultAsync(a => a.Id == albumId);

            if (album == null)
                return NotFound("Альбом не найден.");

            var addedCount = 0;
            foreach (var track in album.Tracks)
            {
                if (!user.Tracks.Any(t => t.Id == track.Id))
                {
                    user.Tracks.Add(track);
                    addedCount++;
                }
            }

            if (addedCount == 0)
                return BadRequest("Все треки этого альбома уже добавлены в медиатеку.");

            await _context.SaveChangesAsync();
            return Ok($"Добавлено треков: {addedCount}");
        }

        [HttpGet("playlists/others")]
        public async Task<IActionResult> GetPublicPlaylistsFromOthers()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var playlists = await _context.Playlists
                .Where(p => p.CreatorId != userId && p.Type == "public")
                .Include(p => p.Creator)
                .Select(p => new {
                    p.Id,
                    p.Title,
                    p.CoverUrl,
                    p.Description,
                    p.CreatedAt,
                    p.Type,
                    Creator = p.Creator!.Username
                })
                .ToListAsync();

            return Ok(playlists);
        }

        [HttpGet("library/saved_playlists")]
        public async Task<IActionResult> GetSavedPlaylists()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var savedPlaylists = await _context.UserPlaylists
                .Where(up => up.UserId == userId)
                .Include(up => up.Playlist)
                    .ThenInclude(p => p.Creator)
                .Select(up => new {
                    up.Playlist.Id,
                    up.Playlist.Title,
                    up.Playlist.CoverUrl,
                    up.Playlist.Description,
                    up.Playlist.CreatedAt,
                    up.Playlist.Type,
                    Creator = up.Playlist.Creator!.Username
                })
                .ToListAsync();

            return Ok(savedPlaylists);
        }

        [HttpPost("library/add_playlist/{playlistId}")]
        public async Task<IActionResult> SavePlaylistToLibrary(int playlistId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var playlist = await _context.Playlists.FindAsync(playlistId);
            if (playlist == null)
                return NotFound("Плейлист не найден.");

            var alreadySaved = await _context.UserPlaylists.AnyAsync(up =>
                up.UserId == userId && up.PlaylistId == playlistId && (up.IsOwner == false || up.IsOwner == null));
            if (alreadySaved)
                return BadRequest("Плейлист уже сохранён в медиатеке.");

            var userPlaylist = new UserPlaylist
            {
                UserId = userId.Value,
                PlaylistId = playlistId,
                IsOwner = false
            };

            _context.UserPlaylists.Add(userPlaylist);
            await _context.SaveChangesAsync();

            return Ok("Плейлист успешно добавлен в медиатеку.");
        }

        [HttpDelete("library/remove_playlist/{playlistId}")]
        public async Task<IActionResult> RemoveSavedPlaylist(int playlistId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var link = await _context.UserPlaylists
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PlaylistId == playlistId && up.IsOwner == false);

            if (link == null)
                return NotFound("Плейлист не найден в медиатеке.");

            _context.UserPlaylists.Remove(link);
            await _context.SaveChangesAsync();

            return Ok("Плейлист удалён из медиатеки.");
        }

        [HttpPost("add_track")]
        public async Task<ActionResult<Track>> AddTrack([FromBody] TrackDTO trackDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized("Пользователь не найден.");

            if (trackDto.AlbumId.HasValue && !await _context.Albums.AnyAsync(a => a.Id == trackDto.AlbumId))
                return BadRequest($"Альбом с ID {trackDto.AlbumId} не найден.");

            if (trackDto.GenreId.HasValue && !await _context.Genres.AnyAsync(g => g.Id == trackDto.GenreId))
                return BadRequest($"Жанр с ID {trackDto.GenreId} не найден.");

            var singer = await _context.Singers
                .FirstOrDefaultAsync(s => s.Name.ToLower() == user.Username.ToLower());

            if (singer == null)
            {
                singer = new Singer
                {
                    Name = user.Username,
                    PhotoUrl = "",
                    Description = "Пользовательский исполнитель",
                    ViewCount = 0,
                    SubscribersCount = 0
                };

                _context.Singers.Add(singer);
                await _context.SaveChangesAsync();
            }

            var releaseDate = DateOnly.FromDateTime(DateTime.Now.ToLocalTime());

            var newTrack = new Track
            {
                Name = trackDto.Name,
                AlbumId = trackDto.AlbumId,
                Duration = trackDto.Duration,
                GenreId = trackDto.GenreId,
                ReleaseDate = releaseDate,
                PlayCount = trackDto.PlayCount ?? 0,
                AudioUrl = trackDto.AudioUrl,
                CoverUrl = trackDto.CoverUrl,
                Status = "Активен", 
                Singers = new List<Singer> { singer }
            };

            _context.Tracks.Add(newTrack);
            await _context.SaveChangesAsync();

            var result = new TrackDTO
            {
                Id = newTrack.Id,
                Name = newTrack.Name,
                AlbumId = newTrack.AlbumId,
                GenreId = newTrack.GenreId,
                Duration = newTrack.Duration,
                AudioUrl = newTrack.AudioUrl,
                CoverUrl = newTrack.CoverUrl,
                Status = newTrack.Status,
                Singers = newTrack.Singers.Select(s => s.Name).ToList()
            };

            return Ok(result);
        }

        private int? GetCurrentUserId()
        {
            if (!Request.Cookies.TryGetValue("userId", out var userIdStr) ||
                !int.TryParse(userIdStr, out var userId))
            {
                return null;
            }

            return userId;
        }
    }
}
