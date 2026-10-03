using System;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Data.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public bool IsActive { get; set; } = true;
        public bool EmailConfirmed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}