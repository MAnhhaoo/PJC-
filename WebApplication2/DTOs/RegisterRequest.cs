namespace WebApplication2.DTOs
{
    public class RegisterRequest
    {
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }

        public string? Role { get; set; }
    }
}