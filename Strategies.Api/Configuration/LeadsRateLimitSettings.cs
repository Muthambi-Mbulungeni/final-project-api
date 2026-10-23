namespace Strategies.Api.Configuration
{
    // "RateLimiting:Leads" section: the public enquiry form (POST /api/leads), per client IP.
    public class LeadsRateLimitSettings
    {
        public const string SectionName = "RateLimiting:Leads";
        public const string PolicyName = "leads-create";

        public int PermitLimit { get; set; } = 5;
        public int WindowSeconds { get; set; } = 600;

        public bool IsValid() => PermitLimit > 0 && WindowSeconds > 0;
    }
}
