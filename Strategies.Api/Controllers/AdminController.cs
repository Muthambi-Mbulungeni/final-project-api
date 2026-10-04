using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
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
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            var totalRecords = await query.CountAsync();
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
                    IsActive = u.IsActive
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
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            if (!user.IsActive)
                return Conflict(new ApiErrorResponse { Message = "User account is already suspended." });

            user.IsActive = false;

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out var adminId);

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

            if (user.IsActive)
                return Conflict(new ApiErrorResponse { Message = "User account is already active." });

            user.IsActive = true;

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out var adminId);

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                Action = "Reinstated user",
                EntityType = "User",
                EntityId = id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "User account has been reinstated." });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            if (!user.IsActive)
                return Conflict(new ApiErrorResponse { Message = "User is already deactivated." });

            user.IsActive = false; // Soft delete

            var adminIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(adminIdStr, out var adminId);

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = adminId,
                Action = "Deactivated user",
                EntityType = "User",
                EntityId = id,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(new { Message = "User soft-deleted successfully (deactivated)." });
        }

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
