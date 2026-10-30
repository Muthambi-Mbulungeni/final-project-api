using System;

namespace Strategies.Api.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        // True only while a sign-in lockout is running; LockoutEndsAt is then when it lifts, otherwise null.
        public bool IsLocked { get; set; }
        public DateTime? LockoutEndsAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}