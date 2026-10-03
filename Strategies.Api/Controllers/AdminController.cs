using System;
using System.Linq;
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

        /// <summary>
        /// GET /api/admin/users
        /// Paginated user list with optional search.
        /// </summary>
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
                    Id       = u.Id,
                    FullName = u.FullName,
                    Email    = u.Email,
                    Role     = u.Role.ToString(),
                    IsActive = u.IsActive
                })
                .ToListAsync();

            return Ok(new PagedResponse<UserDto>
            {
                Data         = users,
                TotalRecords = totalRecords,
                TotalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage  = page
            });
        }

        /// <summary>
        /// PATCH /api/admin/users/{id}/suspend
        /// Soft-suspends a user. Their data and case history is preserved.
        /// </summary>
        [HttpPatch("users/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            if (!user.IsActive)
                return Conflict(new ApiErrorResponse { Message = "User account is already suspended." });

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "User account has been suspended." });
        }

        /// <summary>
        /// PATCH /api/admin/users/{id}/reinstate
        /// Reinstates a previously suspended user.
        /// </summary>
        [HttpPatch("users/{id}/reinstate")]
        public async Task<IActionResult> ReinstateUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new ApiErrorResponse { Message = "User not found." });

            if (user.IsActive)
                return Conflict(new ApiErrorResponse { Message = "User account is already active." });

            user.IsActive = true;
            await _context.SaveChangesAsync();
            return Ok(new { Message = "User account has been reinstated." });
        }

        /// <summary>
        /// DELETE /api/admin/users/{id}
        /// Permanent deletion is not permitted. User records must be retained for
        /// POPIA compliance and case audit trail integrity.
        /// Use PATCH /suspend to deactivate accounts instead.
        /// </summary>
        [HttpDelete("users/{id}")]
        public IActionResult DeleteUser(Guid id)
        {
            return StatusCode(405, new ApiErrorResponse
            {
                Message = "Permanent user deletion is not permitted.",
                Errors  = new System.Collections.Generic.List<string>
                {
                    "User records must be retained for POPIA compliance and case audit trail integrity.",
                    "Use PATCH /api/admin/users/{id}/suspend to deactivate an account."
                }
            });
        }

        /// <summary>
        /// GET /api/admin/audit-logs
        /// Paginated audit log with optional user filter.
        /// </summary>
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
                .ToListAsync();

            return Ok(new PagedResponse<AuditLog>
            {
                Data         = logs,
                TotalRecords = totalRecords,
                TotalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage  = page
            });
        }
    }
}
