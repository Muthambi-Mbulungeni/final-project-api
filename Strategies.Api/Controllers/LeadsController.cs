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
using Strategies.Api.Services;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
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

            var aiResult = await agentService.ProcessLeadAsync(
                $"Name: {dto.FullName}, Immigration status: {dto.ImmigrationStatus}, Needs assistance: {dto.NeedsAssistance}");

            var triageStatus = TriageStatus.NeedsReview;
            string? triageReason = null;

            if (aiResult.Status == RunStatus.Success.ToString() && !string.IsNullOrWhiteSpace(aiResult.ExtractedData))
            {
                try
                {
                    using var jsonDoc = System.Text.Json.JsonDocument.Parse(aiResult.ExtractedData);
                    if (jsonDoc.RootElement.TryGetProperty("status", out var statusElement))
                    {
                        var statusStr = statusElement.GetString();
                        if (Enum.TryParse<TriageStatus>(statusStr, true, out var parsedStatus))
                        {
                            triageStatus = parsedStatus;
                        }
                    }
                    if (jsonDoc.RootElement.TryGetProperty("reason", out var reasonElement))
                    {
                        triageReason = reasonElement.GetString();
                    }
                }
                catch
                {
                    // Fall back to NeedsReview if parsing fails
                }
            }

            var lead = new Lead
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim().ToLowerInvariant(),
                ImmigrationStatus = dto.ImmigrationStatus,
                NeedsAssistance = dto.NeedsAssistance,
                DesiredStartDate = dto.DesiredStartDate,
                VisaCategoryId = dto.VisaCategoryId,
                SubmittedAt = DateTime.UtcNow,
                TriageStatus = triageStatus,
                TriageReason = triageReason
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeads), new { id = lead.Id }, new
            {
                Message = "Lead successfully submitted.",
                LeadId = lead.Id,
                TriageStatus = lead.TriageStatus.ToString(),
                AiAvailable = aiResult.Status == RunStatus.Success.ToString(),
                AiAnalysis = aiResult.Status == RunStatus.Success.ToString() ? aiResult.ExtractedData : null,
                FallbackNote = aiResult.Status != RunStatus.Success.ToString() ? aiResult.Message : null
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