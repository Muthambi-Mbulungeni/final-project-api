using System;
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
    [Route("api/[controller]")]
    [Authorize]
    public class CasesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CasesController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/cases
        /// Paginated case list with display names. Search and stage filter applied
        /// before projection so EF can translate them to SQL — not on client-side strings.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> GetCases(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? stage = null)
        {
            // Build the join query against raw DB columns first.
            // Filters applied HERE so EF translates them to SQL, not after projection.
            var baseQuery = from c in _context.Cases
                            join cl in _context.Users on c.ClientId equals cl.Id into clientGroup
                            from client in clientGroup.DefaultIfEmpty()
                            join co in _context.Users on c.ConsultantId equals co.Id into consultantGroup
                            from consultant in consultantGroup.DefaultIfEmpty()
                            join vc in _context.VisaCategories on c.VisaCategoryId equals vc.Id into vcGroup
                            from visaCat in vcGroup.DefaultIfEmpty()
                            select new { c, client, consultant, visaCat };

            if (!string.IsNullOrWhiteSpace(search))
                baseQuery = baseQuery.Where(x =>
                    (x.client != null && x.client.FullName.Contains(search)) ||
                    (x.visaCat != null && x.visaCat.Name.Contains(search)));

            if (!string.IsNullOrWhiteSpace(stage) &&
                Enum.TryParse<CaseStage>(stage, ignoreCase: true, out var parsedStage))
                baseQuery = baseQuery.Where(x => x.c.Stage == parsedStage);

            if (User.IsInRole("Consultant"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out var currentUserId);
                baseQuery = baseQuery.Where(x => x.c.ConsultantId == currentUserId);
            }

            var totalRecords = await baseQuery.CountAsync();

            var cases = await baseQuery
                .OrderByDescending(x => x.c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CaseDto
                {
                    Id = x.c.Id,
                    ClientId = x.c.ClientId,
                    ClientName = x.client != null ? x.client.FullName : "Unassigned",
                    ConsultantName = x.consultant != null ? x.consultant.FullName : "Unassigned",
                    VisaCategoryName = x.visaCat != null ? x.visaCat.Name : "N/A",
                    Stage = x.c.Stage.ToString(),
                    IsArchived = x.c.IsArchived,
                    CreatedAt = x.c.CreatedAt,
                    LastUpdated = x.c.UpdatedAt
                })
                .ToListAsync();

            return Ok(new PagedResponse<CaseDto>
            {
                Data = cases,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = page
            });
        }

        /// <summary>
        /// POST /api/cases
        /// Creates a new case for a client. Admin and Consultant only.
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> CreateCase([FromBody] CreateCaseRequest request)
        {
            var client = await _context.Users.FindAsync(request.ClientId);
            if (client == null || !client.IsActive)
                return BadRequest(new ApiErrorResponse { Message = "Valid, active client is required." });

            var visaCat = await _context.VisaCategories.FindAsync(request.VisaCategoryId);
            if (visaCat == null || !visaCat.IsActive)
                return BadRequest(new ApiErrorResponse { Message = "Valid, active visa category is required." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out var currentUserId);

            var newCase = new Case
            {
                Id = Guid.NewGuid(),
                ClientId = request.ClientId,
                VisaCategoryId = request.VisaCategoryId,
                LeadId = request.LeadId,
                ConsultantId = User.IsInRole("Consultant") ? currentUserId : null,
                Stage = CaseStage.InitialIntake,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cases.Add(newCase);

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Action = "CreateCase",
                EntityType = "Case",
                EntityId = newCase.Id,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(audit);

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCase), new { id = newCase.Id }, new { Message = "Case created.", CaseId = newCase.Id });
        }

        /// <summary>
        /// GET /api/cases/{id}
        /// Single case with display names.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCase(Guid id)
        {
            if (User.IsInRole("Consultant") || User.IsInRole("Client"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out var currentUserId);
                bool owns;
                if (User.IsInRole("Consultant"))
                    owns = await _context.Cases.AnyAsync(c => c.Id == id && c.ConsultantId == currentUserId);
                else
                    owns = await _context.Cases.AnyAsync(c => c.Id == id && c.ClientId == currentUserId);

                if (!owns) return Forbid();
            }

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

            if (caseRecord == null)
                return NotFound(new ApiErrorResponse { Message = "Case not found." });

            return Ok(caseRecord);
        }

        /// <summary>
        /// GET /api/cases/export
        /// CSV export of all cases. Admin and Consultant only.
        /// </summary>
        [HttpGet("export")]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> ExportCases()
        {
            var baseQuery = _context.Cases.AsQueryable();
            if (User.IsInRole("Consultant"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Guid.TryParse(userIdStr, out var currentUserId);
                baseQuery = baseQuery.Where(c => c.ConsultantId == currentUserId);
            }

            var cases = await (from c in baseQuery
                               join cl in _context.Users on c.ClientId equals cl.Id into cg
                               from client in cg.DefaultIfEmpty()
                               join vc in _context.VisaCategories on c.VisaCategoryId equals vc.Id into vcg
                               from visaCat in vcg.DefaultIfEmpty()
                               orderby c.CreatedAt descending
                               select new
                               {
                                   c.Id,
                                   ClientName = client != null ? client.FullName : "Unassigned",
                                   VisaName = visaCat != null ? visaCat.Name : "N/A",
                                   Stage = c.Stage.ToString(),
                                   c.IsArchived,
                                   c.CreatedAt
                               }).ToListAsync();

            var csv = "Id,Client,VisaCategory,Stage,IsArchived,Created\n" +
                      string.Join("\n", cases.Select(c =>
                          $"{c.Id},{EscapeCsv(c.ClientName)},{EscapeCsv(c.VisaName)},{c.Stage},{c.IsArchived},{c.CreatedAt:O}"));

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", "cases_export.csv");
        }

        /// <summary>
        /// GET /api/cases/client/{clientId}
        /// Client's own cases. Clients can only request their own clientId.
        /// </summary>
        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Admin,Consultant,Client")]
        public async Task<IActionResult> GetClientCases(
            Guid clientId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (User.IsInRole("Client"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdStr, out var currentUserId) || currentUserId != clientId)
                    return Forbid();
            }

            var query = from c in _context.Cases
                        join cl in _context.Users on c.ClientId equals cl.Id into clientGroup
                        from client in clientGroup.DefaultIfEmpty()
                        join co in _context.Users on c.ConsultantId equals co.Id into cog
                        from consultant in cog.DefaultIfEmpty()
                        join vc in _context.VisaCategories on c.VisaCategoryId equals vc.Id into vcg
                        from visaCat in vcg.DefaultIfEmpty()
                        where c.ClientId == clientId
                        select new { c, client, consultant, visaCat };

            var totalRecords = await query.CountAsync();
            var cases = await query
                .OrderByDescending(x => x.c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new CaseDto
                {
                    Id = x.c.Id,
                    ClientId = x.c.ClientId,
                    ClientName = x.client != null ? x.client.FullName : "Unassigned",
                    ConsultantName = x.consultant != null ? x.consultant.FullName : "Unassigned",
                    VisaCategoryName = x.visaCat != null ? x.visaCat.Name : "N/A",
                    Stage = x.c.Stage.ToString(),
                    IsArchived = x.c.IsArchived,
                    CreatedAt = x.c.CreatedAt,
                    LastUpdated = x.c.UpdatedAt
                })
                .ToListAsync();

            return Ok(new PagedResponse<CaseDto>
            {
                Data = cases,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = page
            });
        }

        [HttpPatch("{id}/stage")]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> UpdateStage(Guid id, [FromBody] UpdateCaseStageRequest request)
        {
            if (!request.Stage.HasValue)
                return BadRequest(new ApiErrorResponse { Message = "Stage is required." });

            var c = await _context.Cases.FindAsync(id);
            if (c == null) return NotFound(new ApiErrorResponse { Message = "Case not found." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out var currentUserId);

            if (User.IsInRole("Consultant") && c.ConsultantId != currentUserId)
                return Forbid();

            var oldStage = c.Stage;
            c.Stage = request.Stage.Value;
            c.UpdatedAt = DateTime.UtcNow;

            var history = new CaseStageHistory
            {
                Id = Guid.NewGuid(),
                CaseId = id,
                FromStage = oldStage,
                ToStage = request.Stage.Value,
                ChangedById = currentUserId,
                ChangedAt = DateTime.UtcNow
            };
            _context.CaseStageHistories.Add(history);

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Action = "UpdateCaseStage",
                EntityType = "Case",
                EntityId = id,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(audit);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Stage updated." });
        }

        [HttpPatch("{id}/assign")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignConsultant(Guid id, [FromBody] AssignConsultantRequest request)
        {
            var c = await _context.Cases.FindAsync(id);
            if (c == null) return NotFound(new ApiErrorResponse { Message = "Case not found." });

            c.ConsultantId = request.ConsultantId;
            c.UpdatedAt = DateTime.UtcNow;

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out var currentUserId);

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Action = "AssignConsultant",
                EntityType = "Case",
                EntityId = id,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(audit);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Consultant assigned." });
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }
    }
}