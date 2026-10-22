using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Strategies.Api.Data;

namespace Strategies.Api.Services
{
    // Database check for GET /health, built on the framework's health checks so no extra package
    // is needed (AddDbContextCheck lives in a separate NuGet package). The result never carries
    // exception details: /health is anonymous.
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly ApplicationDbContext _context;

        public DatabaseHealthCheck(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("Database reachable.")
                    : HealthCheckResult.Unhealthy("Database unreachable.");
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                return HealthCheckResult.Unhealthy("Database unreachable.");
            }
        }
    }
}
