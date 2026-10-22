using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Strategies.Api.Configuration
{
    // Settings production cannot run without. Outside Development they have no fallback in
    // appsettings.json, so a missed App Service setting stops startup with one clear message
    // instead of the API running on a default.
    public static class RequiredSettings
    {
        public static readonly string[] Keys =
        {
            "ConnectionStrings:DefaultConnection",
            "Jwt:Key",
            "App:BaseUrl"
        };

        public static void EnsurePresent(IConfiguration configuration, IHostEnvironment environment)
        {
            if (environment.IsDevelopment()) return;

            var missing = Keys.Where(key => string.IsNullOrWhiteSpace(configuration[key])).ToList();
            if (CorsSettings.ReadOrigins(configuration).Length == 0)
                missing.Add(CorsSettings.SectionName);

            if (missing.Count == 0) return;

            // Environment-variable form, as typed into App Service settings (arrays need an index).
            var asSettings = missing.Select(key => key == CorsSettings.SectionName
                ? "Cors__AllowedOrigins__0"
                : key.Replace(":", "__"));

            throw new InvalidOperationException(
                $"Missing required configuration for the '{environment.EnvironmentName}' environment: " +
                $"{string.Join(", ", missing)}. Set them as application settings: {string.Join(", ", asSettings)}.");
        }
    }
}
