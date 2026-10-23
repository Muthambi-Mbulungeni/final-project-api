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
        // Set when an admin deletes the account (soft delete). Suspend only clears IsActive.
        public DateTime? DeletedAt { get; set; }

        // Sign-in lockout. FailedLoginCount counts failed sign-ins since FailedLoginWindowStart; once it
        // reaches the limit, LockoutEndsAt is set and sign-in is refused until then. A successful
        // sign-in, a completed password reset or an admin unlock clears all three.
        public int FailedLoginCount { get; set; }
        public DateTime? FailedLoginWindowStart { get; set; }
        public DateTime? LockoutEndsAt { get; set; }
    }
}