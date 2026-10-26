using System;

namespace Strategies.Api.Configuration
{
    // "Auth:Lockout" section: per-account sign-in lockout, on top of the per-IP sign-in rate limit.
    // MaxAttempts failed sign-ins within WindowMinutes lock the account for DurationMinutes.
    public class LockoutSettings
    {
        public const string SectionName = "Auth:Lockout";

        public int MaxAttempts { get; set; } = 5;
        public int WindowMinutes { get; set; } = 15;
        public int DurationMinutes { get; set; } = 15;

        public TimeSpan Window => TimeSpan.FromMinutes(WindowMinutes);
        public TimeSpan Duration => TimeSpan.FromMinutes(DurationMinutes);

        public bool IsValid() => MaxAttempts > 0 && WindowMinutes > 0 && DurationMinutes > 0;
    }
}
