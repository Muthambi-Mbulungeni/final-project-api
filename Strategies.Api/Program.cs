using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Strategies.Api.Data;
using Strategies.Api.DTOs;
using Strategies.Api.Middleware;
using Strategies.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Context (PostgreSQL EF Core)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// 2. Register Application Security Services
builder.Services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddSingleton<IRefreshTokenStore, InMemoryRefreshTokenStore>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 3. Register Core Domain & AI Services (Marco)
builder.Services.AddHttpClient<IAgentService, GeminiAgentService>();
builder.Services.AddScoped<IAgentService, GeminiAgentService>();
builder.Services.AddScoped<CaseManagementService>();

// 4. JWT Authentication Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "StrategiesMigrationPlatform_SuperSecretKey_2026_WIL_Section10_Token_Security!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "StrategiesApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "StrategiesClients";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
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

    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            var error = new ApiErrorResponse
            {
                Message = "Unauthorized access.",
                Errors = new List<string> { "Authentication token is missing, expired, or invalid." }
            };
            await context.Response.WriteAsJsonAsync(error);
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            var error = new ApiErrorResponse
            {
                Message = "Forbidden.",
                Errors = new List<string> { "You do not have the required permissions to access this resource." }
            };
            await context.Response.WriteAsJsonAsync(error);
        }
    };
});

// 5. Role-Based Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireConsultantRole", policy => policy.RequireRole("Admin", "Consultant"));
    options.AddPolicy("RequireClientRole", policy => policy.RequireRole("Admin", "Consultant", "Client"));
});

// 6. Native ASP.NET Core Rate Limiting Middleware (Partitioned per Client IP)
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new ApiErrorResponse
        {
            Message = "Too many requests.",
            Errors = new List<string> { "Rate limit exceeded. Please wait before making more requests." }
        }, cancellationToken);
    };

    // Strict limit: 5 requests per minute per IP for login and password reset
    options.AddPolicy("auth-strict", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "global-auth";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        });
    });

    // Sliding window limit: 60 requests per minute per IP for general API stability
    options.AddPolicy("sliding-general", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "global-api";
        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 60,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 6,
            QueueLimit = 2
        });
    });
});

// 7. CORS Policy (Unblocks Riaad's Vite 5173 and CRA 3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 8. Add Controllers & String Enum Serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Strategies Migration Platform API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

// 9. Security Headers
app.UseSecurityHeaders();

// 10. Enable Swagger UI in Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Strategies Migration Platform API v1");
    });
}

// 11. CORS must be before Authentication & Authorization
app.UseCors("AllowFrontend");

// 12. Rate Limiting Middleware
app.UseRateLimiter();

// 13. Authentication & Authorization Middleware
app.UseAuthentication();
app.UseAuthorization();

// 14. Map Controller Routes
app.MapControllers();

app.Run();
