namespace NOVER_Back.Models.DTOs
{
    public class UserDTO
    {
        public int Id { get; set; }

        public string Username { get; set; } = null!;

        public string Login { get; set; } = null!;

        public string PasswordHash { get; set; } = null!;

        public string? Avatar { get; set; }

    }
}
