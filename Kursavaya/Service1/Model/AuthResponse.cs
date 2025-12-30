namespace AuthService.Models
{
    public class AuthResponse
    {
        public string Token { get; set; } = default!;
        public Guid UserId { get; set; }
        public string Email { get; set; } = default!;
        public string? Role { get; set; }
        public string UserName { get; set; } = default!;
    }
}
