using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;
using NOVER_Back.Models.DTOs;
using TagLib;


namespace NOVER_Back.Controllers
{
    [Route("api/user")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly DbNoverContext _context;

        public UserController(DbNoverContext context)
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

            if (user.Status == "Заблокирован")
                return Conflict("Пользователь заблокирован в системе");

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

        [HttpPost("update")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<User>> UpdateProfile([FromForm] UserDTO userToUpdate)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Пользователь не найден.");

            if (!string.IsNullOrEmpty(userToUpdate.Login) && user.Login != userToUpdate.Login)
            {
                var loginExists = await _context.Users.AnyAsync(u => u.Login == userToUpdate.Login);
                if (loginExists)
                    return Conflict("Пользователь с таким логином уже существует.");
                user.Login = userToUpdate.Login;
            }

            if (!string.IsNullOrEmpty(userToUpdate.Username))
                user.Username = userToUpdate.Username;

            if (userToUpdate.AvatarFile != null && userToUpdate.AvatarFile.Length > 0)
            {
                var folder = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "UserCovers");
                Directory.CreateDirectory(folder);

                var originalFileName = Path.GetFileName(userToUpdate.AvatarFile.FileName);
                var filePath = Path.Combine(folder, originalFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await userToUpdate.AvatarFile.CopyToAsync(stream);
                }

                user.Avatar = originalFileName;
            }

            if (!string.IsNullOrWhiteSpace(userToUpdate.PasswordHash))
            {
                var hasher = new PasswordHasher<User>();
                user.PasswordHash = hasher.HashPassword(user, userToUpdate.PasswordHash);
            }

            await _context.SaveChangesAsync();
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
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<User>> SignUp([FromForm] UserDTO user)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _context.Users.AnyAsync(u => u.Login == user.Login || u.Username == user.Username);
            if (existingUser)
                return Conflict("Пользователь с таким именем или логином уже существует.");

            string? avatarFileName = null;
            if (user.AvatarFile != null)
            {
                var avatarFolder = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "UserCovers");
                Directory.CreateDirectory(avatarFolder);

                var originalFileName = Path.GetFileName(user.AvatarFile.FileName);
                var avatarPath = Path.Combine(avatarFolder, originalFileName);

                using (var avatarFileStream = new FileStream(avatarPath, FileMode.Create))
                {
                    await user.AvatarFile.CopyToAsync(avatarFileStream);
                }

                avatarFileName = originalFileName;
            }

            var passwordHasher = new PasswordHasher<User>();
            var newUser = new User
            {
                Username = user.Username,
                Login = user.Login,
                Avatar = avatarFileName,
                RegistrationDate = DateTime.UtcNow.ToLocalTime(),
                Role = "Пользователь",
                Status = "Активен",
                PasswordHash = passwordHasher.HashPassword(null!, user.PasswordHash!)
            };

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

            var trackDTOs = user.Tracks
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
                        .Select(s => new SingerDTO
                        {
                            Id = s.Id,
                            Name = s.Name
                        })
                        .ToList(),
                })
                .ToList();

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

        [HttpDelete("library/remove_track/{trackId}")]
        public async Task<IActionResult> RemoveTrackFromLibrary(int trackId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Tracks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            var track = user.Tracks.FirstOrDefault(t => t.Id == trackId);
            if (track == null)
                return NotFound("Трек не найден в медиатеке пользователя.");

            user.Tracks.Remove(track);
            await _context.SaveChangesAsync();

            return Ok("Трек удалён из медиатеки.");
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
                    CreatorRole = up.Playlist.Creator?.Role,
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
                    CreatorRole = up.Playlist.Creator?.Role,
                    IsOwner = false
                })
            };

            return Ok(result);
        }

        [HttpGet("library/albums")]
        public async Task<IActionResult> GetUserLibraryAlbums()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Tracks)
                    .ThenInclude(t => t.Album)
                .Include(u => u.Tracks)
                    .ThenInclude(t => t.Singers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            var albumGroups = user.Tracks
                .Where(t => t.Album != null)
                .GroupBy(t => t.Album)
                .Select(a => new
                {
                    Id = a.Key!.Id,
                    Name = a.Key!.Name,
                    CoverUrl = a.Key.CoverUrl,
                    ReleaseDate = a.Key.ReleaseDate,
                    Singer = a.First().Singers.FirstOrDefault()?.Name ?? "Неизвестно",
                    TrackCount = a.Count()
                })
                .ToList();

            return Ok(albumGroups);
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

        [HttpDelete("library/remove_album/{albumId}")]
        public async Task<IActionResult> RemoveAlbumFromLibrary(int albumId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Tracks)
                .ThenInclude(t => t.Album)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            var album = await _context.Albums
                .Include(a => a.Tracks)
                .FirstOrDefaultAsync(a => a.Id == albumId);

            if (album == null)
                return NotFound("Альбом не найден.");

            var tracksToRemove = album.Tracks
                .Where(t => user.Tracks.Any(ut => ut.Id == t.Id))
                .ToList();

            if (tracksToRemove.Count == 0)
                return BadRequest("Треки этого альбома отсутствуют в медиатеке пользователя.");

            foreach (var track in tracksToRemove)
            {
                user.Tracks.Remove(track);
            }

            await _context.SaveChangesAsync();

            return Ok($"Удалено треков из медиатеки: {tracksToRemove.Count}");
        }


        [HttpGet("playlists/others")]
        public async Task<IActionResult> GetPublicPlaylistsFromOthers()
        {
            var playlists = await _context.Playlists
                .Where(p => p.Type == "public")
                .Include(p => p.Creator)
                .Select(p => new {
                    p.Id,
                    p.Title,
                    p.CoverUrl,
                    p.Description,
                    p.CreatedAt,
                    p.Type,
                    Creator = p.Creator!.Username,
                    CreatorRole = p.Creator!.Role,
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
                    Creator = up.Playlist.Creator!.Username,
                    CreatorRole = up.Playlist.Creator!.Role,
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
                .FirstOrDefaultAsync(up => up.UserId == userId && up.PlaylistId == playlistId);

            if (link == null)
                return NotFound("Плейлист не найден в медиатеке.");

            _context.UserPlaylists.Remove(link);
            await _context.SaveChangesAsync();

            return Ok("Плейлист удалён из медиатеки.");
        }

        [HttpPost("publish_track")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(TrackDTO), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishTrack([FromForm] PublishTrackRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Аудиофайл не загружен.");

            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized("Пользователь не найден.");

            if (request.AlbumId.HasValue && !await _context.Albums.AnyAsync(a => a.Id == request.AlbumId))
                return BadRequest($"Альбом с ID {request.AlbumId} не найден.");

            if (request.GenreId.HasValue && !await _context.Genres.AnyAsync(g => g.Id == request.GenreId))
                return BadRequest($"Жанр с ID {request.GenreId} не найден.");

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "trackAudios");
            Directory.CreateDirectory(uploadsFolder);

            var originalName = Path.GetFileNameWithoutExtension(request.File.FileName);
            var ext = Path.GetExtension(request.File.FileName);
            var safeName = $"{originalName}{ext}";

            var filePath = Path.Combine(uploadsFolder, safeName);

            using (var saveStream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(saveStream);
            }

            var singer = await _context.Singers
                .FirstOrDefaultAsync(s => s.Name.ToLower() == user.Username.ToLower());

            if (singer == null)
            {
                singer = new Singer
                {
                    Name = user.Username,
                    PhotoUrl = user.Avatar,
                    Description = "Пользовательский исполнитель",
                    ViewCount = 0,
                    SubscribersCount = 0
                };

                _context.Singers.Add(singer);
                await _context.SaveChangesAsync();
            }

            var releaseDate = DateOnly.FromDateTime(DateTime.Now.ToLocalTime());

            using var audioStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var abstraction = new StreamFileAbstraction(filePath, audioStream, audioStream);
            var tfile = TagLib.File.Create(abstraction);

            var durationInSeconds = (int)tfile.Properties.Duration.TotalSeconds;

            string? coverFileName = null;
            if (request.CoverFile != null)
            {
                var coverFolder = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "TrackCovers");
                Directory.CreateDirectory(coverFolder);

                var originalFileName = Path.GetFileName(request.CoverFile.FileName);
                var coverPath = Path.Combine(coverFolder, originalFileName);

                using (var coverStream = new FileStream(coverPath, FileMode.Create))
                {
                    await request.CoverFile.CopyToAsync(coverStream);
                }

                coverFileName = originalFileName;
            }

            var newTrack = new Track
            {
                Name = request.Name,
                AlbumId = request.AlbumId,
                Duration = durationInSeconds,
                GenreId = request.GenreId,
                ReleaseDate = releaseDate,
                AudioUrl = safeName,
                CoverUrl = coverFileName,
                Status = "На модерации",
                Singers = new List<Singer> { singer }
            };

            _context.Tracks.Add(newTrack);
            await _context.SaveChangesAsync();

            var result = new TrackDTO
            {
                Name = newTrack.Name,
                AlbumId = newTrack.AlbumId,
                GenreId = newTrack.GenreId,
                Duration = newTrack.Duration,
                AudioUrl = newTrack.AudioUrl,
                CoverUrl = newTrack.CoverUrl,
                Status = newTrack.Status,
                Singers = newTrack.Singers
                    .Where(s => s.Status == "Активен")
                    .Select(s => new SingerDTO
                    {
                        Id = s.Id,
                        Name = s.Name
                    })
                    .ToList()
            };

            return Ok(result);
        }

        [HttpPost("publish_album")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> PublishAlbum([FromForm] PublishAlbumRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return Unauthorized("Пользователь не найден.");

            var singer = await _context.Singers
                .FirstOrDefaultAsync(s => s.Name.ToLower() == user.Username.ToLower());

            if (singer == null)
            {
                singer = new Singer
                {
                    Name = user.Username,
                    PhotoUrl = user.Avatar,
                    Description = "Пользовательский исполнитель",
                    ViewCount = 0,
                    SubscribersCount = 0
                };
                _context.Singers.Add(singer);
                await _context.SaveChangesAsync();
            }

            if (string.IsNullOrWhiteSpace(request.AlbumName))
                return BadRequest("Название альбома не указано.");

            string? coverFileName = null;
            if (request.CoverFile != null)
            {
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "AlbumCovers");
                Directory.CreateDirectory(folderPath);

                var originalFileName = Path.GetFileName(request.CoverFile.FileName);
                var filePath = Path.Combine(folderPath, originalFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.CoverFile.CopyToAsync(stream);
                }

                coverFileName = originalFileName;
            }


            var album = new Album
            {
                Name = request.AlbumName,
                SingerId = singer.Id,
                CoverUrl = coverFileName,
                ReleaseDate = DateOnly.FromDateTime(DateTime.Now.ToLocalTime())
            };

            _context.Albums.Add(album);
            await _context.SaveChangesAsync();

            return Ok(new { album.Id, Message = "Альбом успешно опубликован." });
        }

        [HttpGet("subscriptions")]
        public async Task<IActionResult> GetMySubscriptions()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Singers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            var result = user.Singers.Select(s => new SingerDTO
            {
                Id = s.Id,
                Name = s.Name,
                PhotoUrl = s.PhotoUrl,
                Description = s.Description,
                ViewCount = s.ViewCount,
                SubscribersCount = s.SubscribersCount
            }).ToList();

            if (!result.Any())
                return NotFound("Пользователь ни на кого не подписан.");

            return Ok(result);
        }

        [HttpGet("subscribed_albums")]
        public async Task<IActionResult> GetSubscribedAlbums()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Singers)
                    .ThenInclude(s => s.Albums)
                        .ThenInclude(a => a.Tracks)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");
            
            var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));

            var albums = user.Singers
                .SelectMany(s => s.Albums)
                .Where(a =>
                    a.ReleaseDate != null && a.ReleaseDate >= cutoffDate &&
                    a.Tracks.Any(t => t.Status == "Активен") &&
                    a.Singer!.Status == "Активен"
                )
                .OrderByDescending(a => a.ReleaseDate)
                .Take(15)
                .Select(album => new
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
                        Status = t.Status
                    })
                })
                .ToList();

            return Ok(albums);
        }

        [HttpGet("subscribed_tracks")]
        public async Task<IActionResult> GetSubscribedTracks()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var user = await _context.Users
                .Include(u => u.Singers)
                .ThenInclude(s => s.Tracks)
                .ThenInclude(t => t.Singers)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return NotFound("Пользователь не найден.");

            var cutoffDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-6));

            var tracks = user.Singers
                .SelectMany(s => s.Tracks)
                .Where(t =>
                    t.ReleaseDate != null &&
                    t.ReleaseDate >= cutoffDate &&
                    t.Status == "Активен" &&
                    t.Singers.All(s => s.Status == "Активен") &&
                    (t.Genre == null || t.Genre.Status == "Активен")
                )
                .OrderByDescending(t => t.PlayCount ?? 0)
                .Distinct()
                .Take(50)
                .Select(t => new
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
                        .Select(s => new SingerDTO
                        {
                            Id = s.Id,
                            Name = s.Name
                        })
                        .ToList()
                })
                .ToList();

            return Ok(tracks);
        }

        [HttpPost("review")]
        public async Task<IActionResult> Review([FromBody] ReviewDTO review)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var track = await _context.Tracks.FindAsync(review.TrackId);
            if (track == null)
                return NotFound("Трек не найден.");

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.TrackId == review.TrackId);

            if (existingRating != null)
            {
                return BadRequest("Вы уже оценили этот трек.");
            }

            _context.Ratings.Add(new Rating
            {
                UserId = userId.Value,
                TrackId = review.TrackId,
                Rating1 = review.Rating
            });

            if (!string.IsNullOrWhiteSpace(review.Comment))
            {
                _context.Comments.Add(new Comment
                {
                    UserId = userId.Value,
                    TrackId = review.TrackId,
                    CommentText = review.Comment,
                    CreatedAt = DateTime.UtcNow.ToLocalTime()
                });
            }

            await _context.SaveChangesAsync();
            return Ok("Отзыв успешно сохранён.");
        }

        [HttpPut("update_review")]
        public async Task<IActionResult> UpdateReview([FromBody] ReviewDTO review)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var track = await _context.Tracks.FindAsync(review.TrackId);
            if (track == null)
                return NotFound("Трек не найден.");

            var existingRating = await _context.Ratings
                .FirstOrDefaultAsync(r => r.UserId == userId && r.TrackId == review.TrackId);

            if (existingRating != null)
            {
                existingRating.Rating1 = review.Rating;
            }
            else
            {
                return BadRequest("Вы ещё не оценили этот трек.");
            }

            if (!string.IsNullOrWhiteSpace(review.Comment))
            {
                var existingComment = await _context.Comments
                    .Where(c => c.UserId == userId && c.TrackId == review.TrackId)
                    .OrderByDescending(c => c.CreatedAt)
                    .FirstOrDefaultAsync();

                if (existingComment != null)
                {
                    existingComment.CommentText = review.Comment;
                    existingComment.CreatedAt = DateTime.UtcNow.ToLocalTime();
                    existingComment.Status = "Активен";
                }
                else
                {
                    _context.Comments.Add(new Comment
                    {
                        UserId = userId.Value,
                        TrackId = review.TrackId,
                        CommentText = review.Comment,
                        CreatedAt = DateTime.UtcNow.ToLocalTime(),
                        Status = "Активен"
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok("Отзыв обновлён.");
        }

        [HttpGet("review_exists/{trackId}")]
        public async Task<IActionResult> ReviewExists(int trackId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized("Пользователь не авторизован.");

            var hasReview = await _context.Ratings
                .AnyAsync(r => r.UserId == userId && r.TrackId == trackId);

            return Ok(hasReview);
        }

        [HttpPost("block_review")]
        public async Task<IActionResult> BlockReview([FromBody] ReviewDTO review)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var existingComment = await _context.Comments.FirstOrDefaultAsync(c => c.UserId == userId && c.TrackId == review.TrackId);

            if (existingComment == null) { return NotFound("Комментарий не найден"); }
            
            existingComment.Status = "Заблокирован";

            await _context.SaveChangesAsync();
            return Ok("Комментарий заблокирован.");
        }

        [HttpPost("add_complaint")]
        public async Task<IActionResult> AddComplaint([FromBody] ComplaintDTO complaintDto)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var track = await _context.Tracks.FindAsync(complaintDto.TrackId);
            if (track == null)
                return NotFound($"Трек с ID {complaintDto.TrackId} не найден.");

            var alreadyComplained = await _context.Complaints
                .AnyAsync(c => c.UserId == userId.Value && c.TrackId == complaintDto.TrackId);
            if (alreadyComplained)
                return BadRequest("Вы уже отправляли жалобу на этот трек.");

            if (string.IsNullOrWhiteSpace(complaintDto.Content))
                return BadRequest("Текст жалобы не может быть пустым.");

            var complaint = new Complaint
            {
                UserId = userId.Value,
                TrackId = complaintDto.TrackId,
                Content = complaintDto.Content.Trim(),
                CreatedAt = DateTime.UtcNow.ToLocalTime(),
                Status = "На рассмотрении" 
            };

            _context.Complaints.Add(complaint);
            await _context.SaveChangesAsync();

            return Ok(complaint);
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
