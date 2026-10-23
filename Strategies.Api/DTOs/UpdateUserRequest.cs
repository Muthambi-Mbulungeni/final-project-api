namespace Strategies.Api.DTOs
{
    // Partial update: a null field is left unchanged. Role is the enum name ("Admin", "Consultant", "Client").
    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}
