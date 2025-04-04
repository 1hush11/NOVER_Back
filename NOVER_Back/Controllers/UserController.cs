using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NOVER_Back.Models;
using NOVER_Back.Models.DTOs;

namespace NOVER_Back.Controllers
{
    [Route("API/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private NoverDbContext _context;
        public UserController(NoverDbContext context)
        {
            _context = context;
        }
        private static User? _currentUser { get; set; }

        [HttpGet("LogIn")]
        public ActionResult<User?> LogIn(string login, string password)
        {
            User? user = _context.Users.Where(u => u.Login == login && u.PasswordHash == password).FirstOrDefault();
            if (user != null)
                _currentUser = user;
            return user == null ? NotFound("Пользователь не найден") : Ok(user);
        }
        [HttpPost("SignUp")]
        public ActionResult<User?> SignUp([FromBody] UserDTO user)
        {
            try
            {
                User? existingUser = _context.Users.FirstOrDefault(u => u.Login == user.Login);
                if (existingUser != null)
                {
                    return Conflict($"Пользователь с логином {user?.Login} уже существует.");
                }

                User newUser = new User
                {
                    Username = user.Username,
                    Login = user.Login,
                    PasswordHash = user.PasswordHash,
                    Avatar = user.Avatar,
                    RegistrationDate = user.RegistrationDate,
                    Role = user.Role,
                    Status = user.Status,
                };

                _currentUser = newUser;

                _context.Users.Add(newUser);
                _context.SaveChanges();

                return Ok(newUser);
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, $"Ошибка базы данных: {dbEx.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Произошла ошибка: {ex.Message}");
            }
        }
        [HttpGet("GetCurrentUser")]
        public ActionResult<User?> GetCurrentUser()
        {
            if (_currentUser != null)
            {
                User? user = _context.Users.FirstOrDefault(u => u.Id == _currentUser.Id);
                return user == null ? NotFound("Пользователь не найден.") : Ok(user);
            }
            else
                return NotFound();
        }
        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] UserDTO updatedUser)
        {
            if (updatedUser == null || string.IsNullOrWhiteSpace(updatedUser.Login))
            {
                return BadRequest("Некорректные данные пользователя");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == updatedUser.Id);

            if (user == null)
            {
                return NotFound($"Пользователь с таким ID {updatedUser.Id} не найден");
            }

            user.Username = updatedUser.Username ?? user.Username;
            user.Login = updatedUser.Login ?? user.Login;
            user.PasswordHash = updatedUser.PasswordHash ?? user.PasswordHash;
            user.Avatar = updatedUser.Avatar;

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Пользователь успешно обновлен");
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, $"Ошибка при обновлении пользователя: {ex.Message}");
            }
        }
    }
}
