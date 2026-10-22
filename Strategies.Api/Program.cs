using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Strategies.Api.Configuration;
using Strategies.Api.Data;
using Strategies.Api.DTOs;
using Strategies.Api.Middleware;
using Strategies.Api.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Security Services (Kaan)
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPasswordResetTokenService, PasswordResetTokenService>();
builder.Services.AddScoped<IPasswordPolicy, PasswordPolicy>();

// 3. Core Domain & AI Services (Marco - Single clean registration, no duplicates)
builder.Services.AddHttpClient<IAgentService, GeminiAgentService>();
builder.Services.AddScoped<CaseManagementService>();

// 4. JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is not configured. Set it via .NET User Secrets or environment variables.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "StrategiesApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "StrategiesClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 5. Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireConsultantRole", policy => policy.RequireRole("Admin", "Consultant"));
    options.AddPolicy("RequireClientRole", policy => policy.RequireRole("Admin", "Consultant", "Client"));
});

// Forwarded headers: config-driven, off unless "ForwardedHeaders:Enabled" is true (Production).
// See ForwardedHeadersSettings for why only the last X-Forwarded-For entry is trusted.
builder.Services.AddOptions<ForwardedHeadersSettings>()
    .Bind(builder.Configuration.GetSection(ForwardedHeadersSettings.SectionName))
    .Validate(settings => settings.IsValid(), "ForwardedHeaders: ForwardLimit must be at least 1 and every KnownProxies / KnownNetworks entry must be a valid address or CIDR range.")
    .ValidateOnStart();
builder.Services.AddOptions<ForwardedHeadersOptions>()
    .Configure<IOptions<ForwardedHeadersSettings>>((options, settings) => settings.Value.Apply(options));

builder.Services.AddOptions<LeadsRateLimitSettings>()
    .Bind(builder.Configuration.GetSection(LeadsRateLimitSettings.SectionName))
    .Validate(settings => settings.IsValid(), "RateLimiting:Leads: PermitLimit and WindowSeconds must be greater than 0.")
    .ValidateOnStart();

// 6. Rate Limiting (partitioned by client IP, after forwarded headers)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);

        await context.HttpContext.Response.WriteAsJsonAsync(
            new ApiErrorResponse { Message = "Too many requests. Please try again later." }, cancellationToken);
    };

    options.AddPolicy("auth-strict", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            ClientPartitionKey.From(httpContext.Connection.RemoteIpAddress),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Public enquiry form: limits come from "RateLimiting:Leads".
    options.AddPolicy(LeadsRateLimitSettings.PolicyName, httpContext =>
    {
        var settings = httpContext.RequestServices.GetRequiredService<IOptions<LeadsRateLimitSettings>>().Value;
        return RateLimitPartition.GetFixedWindowLimiter(
            ClientPartitionKey.From(httpContext.Connection.RemoteIpAddress),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = settings.PermitLimit,
                Window = TimeSpan.FromSeconds(settings.WindowSeconds),
                QueueLimit = 0
            });
    });
});

// 7. CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// 8. Controllers & JSON
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    // Automatic model validation (missing body, bad JSON, [Required] fields) returns the same
    // ApiErrorResponse shape as the controllers instead of ASP.NET's ProblemDetails.
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = context => new BadRequestObjectResult(
            ApiErrorResponse.FromModelState(context.ModelState)));

// 9. Swagger (Simplified & Error-Free)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// First in the pipeline: everything after it reads the client IP and scheme it sets.
// Azure App Service on Linux sets ASPNETCORE_FORWARDEDHEADERS_ENABLED=true, and the host then adds this
// middleware itself, ahead of this pipeline and with the options configured above. Adding it a second
// time would strip one more X-Forwarded-For entry, trusting a value the caller can make up.
if (!string.Equals(app.Configuration["FORWARDEDHEADERS_ENABLED"], "true", StringComparison.OrdinalIgnoreCase))
    app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors("AllowFrontend");
app.UseSecurityHeaders();
// After routing, so the [EnableRateLimiting] policy on an action is known; before authentication,
// so throttled requests are turned away before any token work.
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Lets the integration tests start the API with WebApplicationFactory<Program>.
public partial class Program { }