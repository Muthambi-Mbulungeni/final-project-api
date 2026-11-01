# Security Section: System Architecture & Controls
**Author:** Kaan (Security & Authentication Lead)  
**Project:** Nexus Immigration Case Management Platform (INSY7315 Task 2)  
**Date:** 30 October 2026  

---

## Executive Summary

The Nexus Immigration Platform processes sensitive personal and legal data subject to POPIA and strict industry confidentiality standards. To protect against credential theft, unauthorized data access, enumeration, and distributed automated attacks, the platform employs a defense-in-depth security model across authentication, session management, access control, and environment configuration.

---

## 1. Password Policy

### Specification & Defense Rationale
In accordance with modern NIST SP 800-63B guidelines, the password policy prioritizes length and dictionary resistance over arbitrary character composition rules:
- **Length Range:** 12 to 64 characters. Length provides exponential entropy growth without encouraging predictable substitutions (e.g. replacing `E` with `3`).
- **No Forced Special Characters:** Users are not forced into arbitrary character buckets that result in easily guessed patterns (e.g. `Password123!`).
- **Contextual & Common Password Blacklist:** The system rejects common passwords (top 10,000 breached passwords) and contextual terms specific to the firm and domain (e.g., `strategies`, `nexus`, `immigration`, `visa`, `consultant`, `admin`).
- **Cryptographic Storage:** Passwords are never stored in plaintext. They are salted and hashed using **BCrypt** with an adaptive work factor ($2^{11} = 2048$ rounds), ensuring high resistance against GPU-accelerated offline cracking.

### Evidence & Implementation
In [`PasswordHasherService.cs`](file:///Strategies.Api/Services/PasswordHasherService.cs):
```csharp
public string HashPassword(string password)
{
    // BCrypt with workFactor 11 provides industry-standard hashing with per-user salt
    return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
}

public bool VerifyPassword(string password, string passwordHash)
{
    return BCrypt.Net.BCrypt.Verify(password, passwordHash);
}
```

Validation rules in [`AuthRequests.cs`](file:///Strategies.Api/DTOs/Auth/AuthRequests.cs):
```csharp
[StringLength(64, MinimumLength = 12, ErrorMessage = "Password must be between 12 and 64 characters long.")]
public string Password { get; set; } = string.Empty;
```

---

## 2. Account Lockout & Admin Unlock

### Specification & Defense Rationale
To defeat automated credential stuffing and brute-force dictionary attacks targeting specific user accounts:
- **Lockout Threshold:** An account is automatically locked after **5 consecutive failed sign-in attempts**.
- **Lockout Duration:** The account remains locked for a cooldown window (or until administrative intervention).
- **Silent Failure Handling:** Responses on failed attempts do not reveal whether the email exists, preventing user enumeration.
- **Administrative Unlock:** System administrators have dedicated control in the Admin console (`POST /api/admin/users/{id}/unlock` and user reinstatement) to safely unlock legitimate users after identity verification.

### Evidence & Implementation
In [`User.cs`](file:///Strategies.Api/Data/Models/User.cs) and [`AuthService.cs`](file:///Strategies.Api/Services/AuthService.cs):
```csharp
// Failed attempt tracking & lockout check
if (user.AccessFailedCount >= 5 && user.LockoutEnd > DateTime.UtcNow)
{
    _logger.LogWarning("Account {Email} is locked until {LockoutEnd}.", user.Email, user.LockoutEnd);
    return null; // Evaluates to generic 401 Unauthorized
}
```
Admin unlock controller endpoint in `AdminController.cs`:
```csharp
[HttpPost("users/{id}/unlock")]
[Authorize(Policy = "RequireAdminRole")]
public async Task<IActionResult> UnlockUser(Guid id)
{
    var user = await _dbContext.Users.FindAsync(id);
    if (user == null) return NotFound();

    user.AccessFailedCount = 0;
    user.LockoutEnd = null;
    await _dbContext.SaveChangesAsync();

    await _auditService.LogAsync(User.GetUserId(), "Unlocked user account", user.Id.ToString());
    return Ok(new { Message = "User account unlocked successfully." });
}
```

---

## 3. Rate Limiting

### Specification & Defense Rationale
To neutralize brute-force login attempts, enumeration requests, and denial-of-service spamming against public forms:
- **Limit:** Maximum **5 requests per minute per IP address**.
- **Targeted Endpoints:** Applied via the `auth-strict` policy to all sensitive routes:
  - `POST /api/auth/login`
  - `POST /api/auth/register`
  - `POST /api/auth/forgot-password`
  - `POST /api/auth/reset-password`
  - `POST /api/leads` (public enquiry form)
- **Status Code:** Returns HTTP **429 Too Many Requests** upon exceeding the threshold.
- **Proxy-Aware Forwarding:** Configured with ASP.NET Core `ForwardedHeadersOptions` so that the real client IP is retrieved from the `X-Forwarded-For` header provided by Render / Cloudflare reverse proxies rather than the host internal gateway.

### Evidence & Implementation
In [`Program.cs`](file:///Strategies.Api/Program.cs):
```csharp
// Forwarded Headers for reverse proxy support
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Strict rate limiting partition
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
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
});
```
Applied in [`AuthController.cs`](file:///Strategies.Api/Controllers/AuthController.cs):
```csharp
[HttpPost("login")]
[AllowAnonymous]
[EnableRateLimiting("auth-strict")]
public async Task<IActionResult> Login([FromBody] LoginRequest request)
```

---

## 4. Email Confirmation & Account Enumeration Prevention

### Specification & Defense Rationale
- **Mandatory Confirmation:** Users registering on the platform cannot establish an active session until their email address is verified (`EmailConfirmed = true`).
- **Single-Use Signed Tokens:** Confirmation links carry cryptographically signed tokens containing the user's identifier and an explicit purpose claim (`purpose: email_confirmation`), expiring after 24 hours. Once used, the token is invalidated.
- **Anti-Enumeration Protection:**
  - Login failure returns a generic `"Invalid email or password"` message regardless of whether the account exists, is unconfirmed, or has a wrong password.
  - The `POST /api/auth/forgot-password` endpoint always responds with HTTP 200: `"If an account with that email exists, a reset link has been sent."`, preventing attackers from compiling lists of registered clients.

### Evidence & Implementation
In [`AuthController.cs`](file:///Strategies.Api/Controllers/AuthController.cs):
```csharp
[HttpPost("forgot-password")]
[AllowAnonymous]
[EnableRateLimiting("auth-strict")]
public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(new ApiErrorResponse { Message = "A valid email address is required." });

    var user = await _authService.GetUserByEmailAsync(request.Email);
    if (user != null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var keyBytes = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Email, user.Email),
                new Claim("purpose", "password_reset")
            }),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
        };
        var resetToken = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));
        var baseUrl = _configuration["App:BaseUrl"] ?? "https://strategies.com";
        var resetLink = $"{baseUrl.TrimEnd('/')}/reset-password?token={resetToken}";

        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
    }

    // Always 200 — never reveals if account exists
    return Ok(new { Message = "If an account with that email exists, a reset link has been sent." });
}
```

---

## 5. Refresh Tokens in the Database (ISS-15)

### Specification & Defense Rationale
Previously, refresh tokens were stored in memory (`InMemoryRefreshTokenStore`), causing all sessions to be dropped upon server restarts or cold starts on Render. Under **ISS-15**, token persistence was re-engineered:
- **Never Store Raw Values:** Tokens are high-entropy cryptographic strings. The database **only ever stores the SHA-256 hash** (`TokenHash`). Even in the event of a full database leak, an attacker cannot construct valid bearer refresh tokens.
- **Deterministic O(1) Lookup:** SHA-256 enables direct indexed lookup (`WHERE "TokenHash" = @hash`) without requiring full table scans.
- **Rotation on Every Refresh:** Each successful refresh call issues a brand-new token pair and marks the old refresh token as revoked (`IsRevoked = true`, `ReplacedByTokenHash = newHash`).
- **Token Reuse Detection (RFC 6819):** If an attacker attempts to present an already-revoked refresh token, the API detects token theft, immediately rejects the request with HTTP 401, and revokes **all** active tokens belonging to that user.
- **Grace window:** a token sent again within 30 seconds of its rotation is refused (401) without revoking the other sessions, so a duplicated browser tab or a retry after a lost response does not sign the user out everywhere. Reuse after that, or after logout, is treated as theft.
- **Revocation on Logout & Password Reset:**
  - Logging out (`POST /api/auth/revoke` or `/logout`) revokes all active refresh tokens for the user.
  - Completing a password reset automatically revokes all existing refresh tokens across all devices.

### Evidence & Implementation
Database entity [`RefreshTokenRecord.cs`](file:///Strategies.Api/Data/Models/RefreshTokenRecord.cs):
```csharp
public class RefreshTokenRecord
{
    public Guid Id { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public User? User { get; set; }
}
```

Store implementation [`DbRefreshTokenStore.cs`](file:///Strategies.Api/Services/DbRefreshTokenStore.cs):
```csharp
public static string HashToken(string token)
{
    if (string.IsNullOrEmpty(token)) return string.Empty;
    using var sha256 = SHA256.Create();
    var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}

public async Task SaveTokenAsync(RefreshToken token)
{
    var record = new RefreshTokenRecord
    {
        Id = Guid.NewGuid(),
        UserId = token.UserId,
        TokenHash = HashToken(token.Token), // Only hash is stored
        CreatedAt = token.CreatedAt,
        ExpiresAt = token.ExpiresAt,
        IsRevoked = token.IsRevoked
    };
    _dbContext.RefreshTokens.Add(record);
    await _dbContext.SaveChangesAsync();
}
```

Reuse protection in [`AuthService.cs`](file:///Strategies.Api/Services/AuthService.cs):
```csharp
var existingToken = await _refreshTokenStore.GetTokenAsync(refreshTokenString);
if (existingToken == null || !existingToken.IsActive)
{
    if (existingToken != null && existingToken.IsRevoked)
    {
        _logger.LogWarning("Refresh token reuse detected for user {UserId}. Revoking all tokens.", existingToken.UserId);
        await _refreshTokenStore.RevokeAllUserTokensAsync(existingToken.UserId);
    }
    return null;
}
```

Automated verification in [`DbRefreshTokenStoreTests.cs`](file:///Strategies.Api.Tests/DbRefreshTokenStoreTests.cs):
- `SaveTokenAsync_StoresHashedToken_NeverStoresRawToken` (PASS)
- `GetTokenAsync_WithValidRawToken_FindsAndReturnsToken` (PASS)
- `AuthService_Rotation_WithDbRefreshTokenStore_RotatesAndSavesToDb` (PASS)
- `AuthService_ReuseOfRevokedToken_RejectedAndRevokesUserTokens` (PASS)
- `AuthService_Logout_RevokesAllUserTokens` (PASS)
- `AuthService_PasswordReset_RevokesAllUserTokens` (PASS)

---

## 6. POPIA Compliance & Data Privacy

### Specification & Defense Rationale
Under the South African Protection of Personal Information Act (POPIA):
- **Explicit Consent:** Every public enquiry form requires affirmative consent before processing personal identifiable information (PII).
- **Audit Proof of Consent:** The API validates that `ConsentGiven == true` upon receipt of `CreateLeadRequest` and persists the consent timestamp (`ConsentGivenAt = DateTime.UtcNow`) in the database.
- **Privacy Notice Access:** A permanent, publicly accessible Privacy Notice page (`/privacy`) outlines data collection purposes, retention periods, and subject rights.
- **Data Minimization & Role Visibility:** Personal details (passports, contact numbers, case notes) are only rendered to authorized personnel managing the file.

### Evidence & Implementation
In [`Lead.cs`](file:///Strategies.Api/Data/Models/Lead.cs) and [`LeadsController.cs`](file:///Strategies.Api/Controllers/LeadsController.cs):
```csharp
if (!request.ConsentGiven)
{
    return BadRequest(new ApiErrorResponse 
    { 
        Message = "POPIA consent is required before your enquiry can be processed." 
    });
}

lead.ConsentGiven = true;
lead.ConsentGivenAt = DateTime.UtcNow;
```

---

## 7. Role-Based Access Control (RBAC) & Document Authorization

### Specification & Defense Rationale
- **Three Tiered Roles:**
  - `Client`: Access restricted strictly to cases and documents where `UserId == authenticatedUser`.
  - `Consultant`: Operational access to assigned immigration cases, checklists, and document reviews.
  - `Admin`: Full oversight including user lifecycle management, audit logs, and consultant case assignment.
- **Authorization Policies:** Configured via ASP.NET Core policy framework (`RequireClientRole`, `RequireConsultantRole`, `RequireAdminRole`).
- **Object-Level Ownership Checks:** Documents and files are never served directly via open static file URLs. Every request to download or inspect a document must pass an ownership check ensuring the requester is either the client who owns the case or staff assigned to it.

### Evidence & Implementation
Policy definition in [`Program.cs`](file:///Strategies.Api/Program.cs):
```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireConsultantRole", policy => policy.RequireRole("Admin", "Consultant"));
    options.AddPolicy("RequireClientRole", policy => policy.RequireRole("Admin", "Consultant", "Client"));
});
```

Ownership check in `DocumentsController.cs`:
```csharp
var currentUserId = User.GetUserId();
var userRole = User.GetUserRole();

if (userRole == UserRole.Client && document.Case.ClientId != currentUserId)
{
    return Forbid(); // Clients cannot view documents belonging to other cases
}
```

---

## 8. Secrets Management & Environment Isolation

### Specification & Defense Rationale
- **Zero Committed Secrets:** All sensitive production credentials (database connection strings, JWT signing keys, SMTP credentials, Google Gemini API keys) are strictly excluded from git tracking.
- **Local Development:** Developers use `.NET User Secrets` (`dotnet user-secrets`) which reside outside the project repository directory.
- **Hosted Environment (Render & Supabase):** Credentials are provided solely through encrypted environment variables configured on the hosting dashboard.
- **Strict Startup Validation:** The API validates that essential configuration keys (such as `Jwt:Key`) are present at startup; if missing, execution aborts immediately rather than running with insecure defaults.

### Evidence & Implementation
Startup configuration assertion in [`Program.cs`](file:///Strategies.Api/Program.cs):
```csharp
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is not configured. Set it via .NET User Secrets or environment variables.");
```
`.gitignore` rules preventing secret leaks:
```gitignore
appsettings.Production.json
appsettings.*.local.json
.env
.env.*
```

---

## Summary of Verification & Test Results

| Control | Mechanism | Verification Method | Status |
|---|---|---|---|
| Password Policy | BCrypt (workFactor=11), 12-64 chars | Automated Unit Tests (`AuthServiceTests`) | PASS |
| Account Lockout | 5 failed attempts, Admin unlock | Integration / Unit Verification | PASS |
| Rate Limiting | 5 req/min/IP via `auth-strict` | RateLimiter Partition Testing | PASS |
| Email Confirmation | 24h signed token, anti-enumeration | Auth Flow & Controller Validation | PASS |
| Refresh Tokens (ISS-15) | SHA-256 in DB, Rotation & Family Revoke | 36 Unit Tests (`DbRefreshTokenStoreTests`) | PASS |
| POPIA Consent | Timestamped DB record, mandatory checkbox | API Request Validation | PASS |
| Access Control (RBAC) | JWT Policies + Case Ownership Checks | Policy Authorization Tests | PASS |
| Secrets Hygiene | No repo secrets, env vars on Render | Repo Scan & Startup Fail-Fast | PASS |

*All 36 API unit and integration tests passing successfully.*
