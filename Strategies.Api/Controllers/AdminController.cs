using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null)
        {
            var query = _context.Users.Where(u => u.DeletedAt == null);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            var totalRecords = await query.CountAsync();
            var now = DateTime.UtcNow;
            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role.ToString(),
                    IsActive = u.IsActive,
                    IsLocked = u.LockoutEndsAt != null && u.LockoutEndsAt > now,
                    LockoutEndsAt = u.LockoutEndsAt > now ? u.LockoutEndsAt : null
                })
                .ToListAsync();

            return Ok(new PagedResponse<UserDto>
            {
                Data = users,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = page
            });
        }

        [HttpPatch("users/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.DeletedAt != null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            if (!user.IsActive)
                return Conflict(new ApiErrorResponse { Message = "User account is already suspended." });

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out var adminId);

            var blocked = await AccessRemovalBlockedAsync(user, adminId, "suspend");
            if (blocked != null)
                return Conflict(new ApiErrorResponse { Message = blocked });

            user.IsActive = false;

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                Action = "Suspended user",
                EntityType = "User",
                EntityId = id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "User account has been suspended." });
        }

        [HttpPatch("users/{id}/reinstate")]
        public async Task<IActionResult> ReinstateUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            if (user.DeletedAt != null)
                return Conflict(new ApiErrorResponse { Message = "Deleted accounts cannot be reinstated." });

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out var adminId);

            // An active account that is only locked out by failed sign-ins can be unlocked the same way.
            var locked = user.LockoutEndsAt != null || user.FailedLoginCount > 0;
            if (user.IsActive && !locked)
                return Conflict(new ApiErrorResponse { Message = "User account is already active." });

            var wasSuspended = !user.IsActive;
            user.IsActive = true;
            user.FailedLoginCount = 0;
            user.FailedLoginWindowStart = null;
            user.LockoutEndsAt = null;

            if (wasSuspended)
                _context.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = adminId,
                    Action = "Reinstated user",
                    EntityType = "User",
                    EntityId = id,
                    CreatedAt = DateTime.UtcNow
                });

            if (locked)
                _context.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = adminId,
                    Action = "Account unlocked",
                    EntityType = "User",
                    EntityId = id,
                    CreatedAt = DateTime.UtcNow
                });

            await _context.SaveChangesAsync();
            return Ok(new { Message = wasSuspended ? "User account has been reinstated." : "User account has been unlocked." });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null || user.DeletedAt != null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out var adminId);

            var blocked = await AccessRemovalBlockedAsync(user, adminId, "delete");
            if (blocked != null)
                return Conflict(new ApiErrorResponse { Message = blocked });

            if (user.Role == UserRole.Consultant)
            {
                var activeCases = await ActiveCaseCountAsync(id);
                if (activeCases > 0)
                    return Conflict(new ApiErrorResponse { Message = ActiveCasesMessage(activeCases, "deleting the account") });
            }

            // Soft delete: the row stays for audit history, but the account is gone for good.
            user.IsActive = false;
            user.DeletedAt = DateTime.UtcNow;

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                Action = "Deleted user",
                EntityType = "User",
                EntityId = id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "User account has been deleted." });
        }

        [HttpPatch("users/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            var errors = new List<string>();
            string? fullName = null;
            string? email = null;
            UserRole? role = null;

            if (request.FullName == null && request.Email == null && request.Role == null)
                errors.Add("Send at least one field to change: fullName, email or role.");

            if (request.FullName != null)
            {
                fullName = request.FullName.Trim();
                if (fullName.Length == 0)
                    errors.Add("Full name is required.");
            }

            if (request.Email != null)
            {
                email = request.Email.Trim().ToLowerInvariant();
                if (email.Any(char.IsWhiteSpace) || !new EmailAddressAttribute().IsValid(email))
                    errors.Add("A valid email address is required.");
            }

            if (request.Role != null)
            {
                var roleName = Enum.GetNames<UserRole>()
                    .FirstOrDefault(n => string.Equals(n, request.Role.Trim(), StringComparison.OrdinalIgnoreCase));
                if (roleName == null)
                    errors.Add("Role must be one of: Admin, Consultant, Client.");
                else
                    role = Enum.Parse<UserRole>(roleName);
            }

            if (errors.Count > 0)
                return BadRequest(new ApiErrorResponse { Message = "Validation failed.", Errors = errors });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && u.DeletedAt == null);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out var adminId);

            // Deleted accounts keep their address, so it still counts as taken.
            if (email != null && email != user.Email.ToLowerInvariant()
                && await _context.Users.AnyAsync(u => u.Id != id && u.Email.ToLower() == email))
                return Conflict(new ApiErrorResponse
                {
                    Message = "Another account already uses this email address.",
                    Code = ApiErrorResponse.DuplicateEmailCode
                });

            if (role.HasValue && role.Value != user.Role)
            {
                if (user.Role == UserRole.Admin)
                {
                    if (id == adminId)
                        return Conflict(new ApiErrorResponse { Message = "You cannot change your own role. Ask another administrator." });

                    if (user.IsActive && !await OtherActiveAdminExistsAsync(id))
                        return Conflict(new ApiErrorResponse { Message = LastAdminMessage });
                }

                if (user.Role == UserRole.Consultant)
                {
                    var activeCases = await ActiveCaseCountAsync(id);
                    if (activeCases > 0)
                        return Conflict(new ApiErrorResponse { Message = ActiveCasesMessage(activeCases, "changing the role") });
                }
            }

            var changes = new List<string>();
            if (fullName != null && fullName != user.FullName)
            {
                changes.Add($"name \"{user.FullName}\" to \"{fullName}\"");
                user.FullName = fullName;
            }
            if (email != null && email != user.Email)
            {
                // EmailConfirmed is left as it is.
                changes.Add($"email {user.Email} to {email}");
                user.Email = email;
            }
            if (role.HasValue && role.Value != user.Role)
            {
                changes.Add($"role {user.Role} to {role.Value}");
                user.Role = role.Value;
            }

            if (changes.Count > 0)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    UserId = adminId,
                    Action = "Updated user: " + string.Join("; ", changes),
                    EntityType = "User",
                    EntityId = id,
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                IsActive = user.IsActive,
                IsLocked = user.LockoutEndsAt > DateTime.UtcNow,
                LockoutEndsAt = user.LockoutEndsAt > DateTime.UtcNow ? user.LockoutEndsAt : null
            });
        }

        private const string LastAdminMessage =
            "This is the last active administrator. Make another user an administrator first.";

        // Suspend and delete must not lock the caller out or leave the system without an active admin.
        private async Task<string?> AccessRemovalBlockedAsync(User user, Guid adminId, string action)
        {
            if (user.Id == adminId)
                return $"You cannot {action} your own account. Ask another administrator.";

            if (user.Role == UserRole.Admin && user.IsActive && !await OtherActiveAdminExistsAsync(user.Id))
                return LastAdminMessage;

            return null;
        }

        private Task<bool> OtherActiveAdminExistsAsync(Guid id) =>
            _context.Users.AnyAsync(u => u.Id != id && u.Role == UserRole.Admin && u.IsActive && u.DeletedAt == null);

        // Same rule as the web portal's isActiveCase: not archived and no final decision yet.
        private Task<int> ActiveCaseCountAsync(Guid consultantId) =>
            _context.Cases.CountAsync(c => c.ConsultantId == consultantId && !c.IsArchived && c.Stage != CaseStage.FinalDecision);

        private static string ActiveCasesMessage(int count, string change) =>
            $"This consultant has {count} active assigned {(count == 1 ? "case" : "cases")}. Reassign them to another consultant before {change}.";

        [HttpGet("audit-logs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? userId = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (userId.HasValue)
                query = query.Where(a => a.UserId == userId.Value);

            var totalRecords = await query.CountAsync();

            var logs = await query
                 .OrderByDescending(a => a.CreatedAt)
                 .Skip((page - 1) * pageSize)
                 .Take(pageSize)
                 .Select(a => new
                 {
                     a.Id,
                     a.UserId,
                     UserName = _context.Users.Where(u => u.Id == a.UserId).Select(u => u.FullName).FirstOrDefault() ?? "System/Unknown",
                     a.Action,
                     a.EntityType,
                     a.EntityId,
                     a.CreatedAt
                 })
                 .ToListAsync();

            return Ok(new
            {
                Data = logs,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = page
            });
        }
    }
}
