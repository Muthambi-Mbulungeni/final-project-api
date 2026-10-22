using System;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Strategies.Api.Configuration
{
    // "Cors:AllowedOrigins": the web portal origins allowed to call the API with credentials.
    // Production sets Cors__AllowedOrigins__0, __1 and so on. The localhost defaults live in
    // appsettings.Development.json only, because configuration arrays merge by index: a default
    // at index 1 in appsettings.json would stay allowed in production next to the real origin.
    public static class CorsSettings
    {
        public const string SectionName = "Cors:AllowedOrigins";
        public const string PolicyName = "AllowFrontend";

        public static string[] ReadOrigins(IConfiguration configuration) =>
            (configuration.GetSection(SectionName).Get<string[]>() ?? Array.Empty<string>())
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim())
                .ToArray();

        // Browsers send Origin as scheme://host[:port] with no path and no trailing slash, and CORS
        // compares it exactly, so anything else in the list would silently never match.
        public static string[] ReadValidatedOrigins(IConfiguration configuration)
        {
            var origins = ReadOrigins(configuration);
            var invalid = origins.Where(origin => !IsValidOrigin(origin)).ToList();
            if (invalid.Count > 0)
                throw new InvalidOperationException(
                    $"{SectionName} entries must be a scheme and host only, such as https://example.github.io " +
                    $"(no path, no trailing slash): {string.Join(", ", invalid)}");
            return origins;
        }

        public static bool IsValidOrigin(string origin) =>
            Uri.TryCreate(origin, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
            string.IsNullOrEmpty(uri.UserInfo) &&
            string.Equals(uri.GetLeftPart(UriPartial.Authority), origin, StringComparison.OrdinalIgnoreCase);
    }
}
