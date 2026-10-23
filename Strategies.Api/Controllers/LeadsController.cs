using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Configuration;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;
using Strategies.Api.Services;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        private const int MaxAgentOutputLength = 2000;
        private const string ConsentRequiredMessage = "You need to agree before we can assess your enquiry.";

        private readonly ApplicationDbContext _context;

        public LeadsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/leads
        /// Paginated, searchable lead list. Admin and Consultant only.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> GetLeads(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null)
        {
            var query = _context.Leads.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(l =>
                    l.FullName.Contains(search) ||
                    l.Email.Contains(search));

            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<TriageStatus>(status, ignoreCase: true, out var parsedStatus))
                query = query.Where(l => l.TriageStatus == parsedStatus);

            var totalRecords = await query.CountAsync();
            var leads = await query
                .OrderByDescending(l => l.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new PagedResponse<Lead>
            {
                Data = leads,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                CurrentPage = page
            });
        }

        /// <summary>
        /// POST /api/leads
        /// Public intake form submission. Uses CreateLeadRequest DTO — never the raw entity.
        /// AI triage runs and sets status. Falls back to NeedsReview if AI is unavailable.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(LeadsRateLimitSettings.PolicyName)]
        public async Task<IActionResult> CreateLead(
            [FromBody] CreateLeadRequest dto,
            [FromServices] IAgentService agentService)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(new ApiErrorResponse { Message = "Validation failed.", Errors = errors });
            }

            // POPIA: no consent, no processing. Checked before anything is sent to the model or stored.
            if (!dto.Consent)
                return BadRequest(new ApiErrorResponse
                {
                    Message = "Validation failed.",
                    Errors = new List<string> { ConsentRequiredMessage },
                    Code = ApiErrorResponse.ConsentRequiredCode
                });

            // Id is generated before the AI call so the AgentRun row can reference it.
            var leadId = Guid.NewGuid();

            string? visaCategoryName = null;
            if (dto.VisaCategoryId.HasValue)
                visaCategoryName = await _context.VisaCategories
                    .Where(c => c.Id == dto.VisaCategoryId.Value)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync();

            // Name and email are not sent to the model (POPIA data minimisation).
            var aiResult = await agentService.TriageLeadAsync(new LeadTriageInput(
                dto.ImmigrationStatus, dto.NeedsAssistance, dto.DesiredStartDate, visaCategoryName));

            var lead = new Lead
            {
                Id = leadId,
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                ImmigrationStatus = dto.ImmigrationStatus,
                NeedsAssistance = dto.NeedsAssistance,
                DesiredStartDate = dto.DesiredStartDate,
                VisaCategoryId = dto.VisaCategoryId,
                SubmittedAt = DateTime.UtcNow,
                ConsentAt = DateTime.UtcNow,
                TriageStatus = aiResult.TriageStatus,
                TriageReason = aiResult.Reason
            };

            var agentRun = new AgentRun
            {
                Id = Guid.NewGuid(),
                LeadId = leadId,
                AgentType = AgentType.Triage,
                Status = aiResult.Status,
                RanAt = DateTime.UtcNow,
                Output = aiResult.Output.Length > MaxAgentOutputLength
                    ? aiResult.Output[..MaxAgentOutputLength]
                    : aiResult.Output
            };

            // One SaveChangesAsync: EF inserts the Lead before the AgentRun because of the LeadId foreign key.
            _context.Leads.Add(lead);
            _context.AgentRuns.Add(agentRun);
            await _context.SaveChangesAsync();

            var aiAvailable = aiResult.Status == RunStatus.Success;
            return CreatedAtAction(nameof(GetLeads), new { id = lead.Id }, new
            {
                Message = "Lead successfully submitted.",
                LeadId = lead.Id,
                TriageStatus = lead.TriageStatus.ToString(),
                AiAvailable = aiAvailable,
                TriageReason = aiAvailable ? lead.TriageReason : null,
                FallbackNote = aiAvailable ? null : aiResult.Message
            });
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> UpdateLeadStatus(Guid id, [FromBody] UpdateLeadStatusRequest request)
        {
            if (!request.Status.HasValue)
                return BadRequest(new ApiErrorResponse { Message = "Status is required." });

            var lead = await _context.Leads.FindAsync(id);
            if (lead == null) return NotFound(new ApiErrorResponse { Message = "Lead not found." });

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out var currentUserId);

            var oldStatus = lead.TriageStatus;
            lead.TriageStatus = request.Status.Value;
            lead.ReviewedById = currentUserId;

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                Action = "UpdateLeadStatus",
                EntityType = "Lead",
                EntityId = id,
                CreatedAt = DateTime.UtcNow
            };
            _context.AuditLogs.Add(audit);

            await _context.SaveChangesAsync();
            return Ok(new { Message = "Lead status updated." });
        }
    }
}