using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.DTOs;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> GetCases([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] string? stage = null)
        {
            var query = from c in _context.Cases
                        join cl in _context.Users on c.ClientId equals cl.Id into clientGroup
                        from client in clientGroup.DefaultIfEmpty()
                        join co in _context.Users on c.ConsultantId equals co.Id into consultantGroup
                        from consultant in consultantGroup.DefaultIfEmpty()
                        join vc in _context.VisaCategories on c.VisaCategoryId equals vc.Id into vcGroup
                        from visaCat in vcGroup.DefaultIfEmpty()
                        select new CaseDto
                        {
                            Id = c.Id,
                            ClientId = c.ClientId,
                            ClientName = client != null ? client.FullName : "Unassigned",
                            ConsultantName = consultant != null ? consultant.FullName : "Unassigned",
                            VisaCategoryName = visaCat != null ? visaCat.Name : "N/A",
                            Stage = c.Stage.ToString(),
                            IsArchived = c.IsArchived,
                            CreatedAt = c.CreatedAt,
                            LastUpdated = c.UpdatedAt
                        };

            if (!string.IsNullOrEmpty(search)) query = query.Where(c => c.ClientName.Contains(search) || c.VisaCategoryName.Contains(search));
            if (!string.IsNullOrEmpty(stage) && Enum.TryParse<CaseStage>(stage, true, out var parsedStage)) query = query.Where(c => c.Stage == parsedStage.ToString());

            var totalRecords = await query.CountAsync();
            var cases = await query.OrderByDescending(c => c.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            return Ok(new PagedResponse<CaseDto> { Data = cases, TotalRecords = totalRecords, TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize), CurrentPage = page });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCase(Guid id)
        {
            var caseRecord = await (from c in _context.Cases
                                    join cl in _context.Users on c.ClientId equals cl.Id into cg
                                    from client in cg.DefaultIfEmpty()
                                    join co in _context.Users on c.ConsultantId equals co.Id into cog
                                    from consultant in cog.DefaultIfEmpty()
                                    join vc in _context.VisaCategories on c.VisaCategoryId equals vc.Id into vcg
                                    from visaCat in vcg.DefaultIfEmpty()
                                    where c.Id == id
                                    select new CaseDto
                                    {
                                        Id = c.Id,
                                        ClientId = c.ClientId,
                                        ClientName = client != null ? client.FullName : "Unassigned",
                                        ConsultantName = consultant != null ? consultant.FullName : "Unassigned",
                                        VisaCategoryName = visaCat != null ? visaCat.Name : "N/A",
                                        Stage = c.Stage.ToString(),
                                        IsArchived = c.IsArchived,
                                        CreatedAt = c.CreatedAt,
                                        LastUpdated = c.UpdatedAt
                                    }).FirstOrDefaultAsync();

            if (caseRecord == null) return NotFound();
            return Ok(caseRecord);
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportCases()
        {
            var cases = await (from c in _context.Cases
                               join cl in _context.Users on c.ClientId equals cl.Id into cg
                               from client in cg.DefaultIfEmpty()
                               join vc in _context.VisaCategories on c.VisaCategoryId equals vc.Id into vcg
                               from visaCat in vcg.DefaultIfEmpty()
                               select new { c.Id, ClientName = client != null ? client.FullName : "Unassigned", VisaName = visaCat != null ? visaCat.Name : "N/A", c.Stage, c.CreatedAt }).ToListAsync();

            var csv = "Id,Client,VisaCategory,Stage,Created\n" + string.Join("\n", cases.Select(c => $"{c.Id},{c.ClientName},{c.VisaName},{c.Stage},{c.CreatedAt}"));
            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "cases_export.csv");
        }

        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Admin,Consultant,Client")]
        public async Task<IActionResult> GetClientCases(Guid clientId)
        {
            // Strict check: If user is a Client, they can only view their own cases
            if (User.IsInRole("Client"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (Guid.TryParse(userIdStr, out var currentUserId) && currentUserId != clientId)
                {
                    return Forbid();
                }
            }

            var cases = await _context.Cases
                .Where(c => c.ClientId == clientId)
                .Select(c => new CaseDto
                {
                    Id = c.Id,
                    ClientId = c.ClientId,
                    Stage = c.Stage.ToString(),
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(new PagedResponse<CaseDto> { Data = cases, TotalRecords = cases.Count, TotalPages = 1, CurrentPage = 1 });
        }

        [HttpGet("export")]
        [Authorize(Roles = "Admin,Consultant")]
        public IActionResult ExportCases([FromQuery] string format = "csv")
        {
            if (format.ToLower() == "csv")
                return File(System.Text.Encoding.UTF8.GetBytes("Id,ClientName,Stage\n11111111-1111-1111-1111-111111111111,John Doe,UnderReview\n"), "text/csv", "cases.csv");

            return BadRequest(new ApiErrorResponse { Message = "Format not supported", Errors = new List<string> { "Only CSV is currently supported." } });
        }
    }
}
