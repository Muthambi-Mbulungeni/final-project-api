using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetCases([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = null, [FromQuery] string stage = null)
        {
            // Manual joins to bypass the missing EF navigation properties
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

            if (!string.IsNullOrEmpty(search))
                query = query.Where(c => c.ClientName.Contains(search) || c.VisaCategoryName.Contains(search));

            if (!string.IsNullOrEmpty(stage) && Enum.TryParse<CaseStage>(stage, true, out var parsedStage))
                query = query.Where(c => c.Stage == parsedStage.ToString());

            var totalRecords = await query.CountAsync();
            var cases = await query.OrderByDescending(c => c.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            return Ok(new PagedResponse<CaseDto> { Data = cases, TotalRecords = totalRecords, TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize), CurrentPage = page });
        }

        [HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetClientCases(Guid clientId)
        {
            var cases = await _context.Cases.Where(c => c.ClientId == clientId).Select(c => new CaseDto { Id = c.Id, ClientId = c.ClientId, Stage = c.Stage.ToString(), CreatedAt = c.CreatedAt }).ToListAsync();
            return Ok(new PagedResponse<CaseDto> { Data = cases, TotalRecords = cases.Count, TotalPages = 1, CurrentPage = 1 });
        }
    }
}