using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = null)
        {
            var query = _context.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            var totalRecords = await query.CountAsync();
            var users = await query.Skip((page - 1) * pageSize).Take(pageSize).Select(u => new UserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsActive = u.IsActive
            }).ToListAsync();

            return Ok(new PagedResponse<UserDto> { Data = users, TotalRecords = totalRecords, TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize), CurrentPage = page });
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsActive = false; // Fixed: Soft delete via IsActive status
            await _context.SaveChangesAsync();
            return Ok(new { Message = "User successfully deactivated." });
        }
    }
}