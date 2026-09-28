using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Strategies.Api.Middleware
{
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                if (!headers.ContainsKey("X-Content-Type-Options"))
                    headers.Append("X-Content-Type-Options", "nosniff");

                if (!headers.ContainsKey("X-Frame-Options"))
                    headers.Append("X-Frame-Options", "DENY");

                if (!headers.ContainsKey("X-XSS-Protection"))
                    headers.Append("X-XSS-Protection", "1; mode=block");

                if (!headers.ContainsKey("Referrer-Policy"))
                    headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

                if (!headers.ContainsKey("Permissions-Policy"))
                    headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        {
            return app.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
