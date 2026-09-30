using System;
using System.Linq;
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
        /// Paginated, searchable lead list. Restricted to Admin and Consultant roles.
        /// Supports filtering by triage status.
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

            if (!string.IsNullOrEmpty(search))
                query = query.Where(l =>
                    l.FullName.Contains(search) ||
                    l.Email.Contains(search));

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TriageStatus>(status, true, out var parsedStatus))
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
        /// Public intake form submission — no login required.
        /// Runs the Gemini AI agent to determine triage status.
        /// Falls back to Pending if AI is unavailable.
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateLead([FromBody] Lead newLead, [FromServices] IAgentService agentService)
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
                $"Name: {newLead.FullName}, Immigration status: {newLead.ImmigrationStatus}, Needs assistance: {newLead.NeedsAssistance}");

            newLead.Id = Guid.NewGuid();
            newLead.SubmittedAt = DateTime.UtcNow;

            // Map AI result to a meaningful triage status with a reason
            if (aiResult.Status == RunStatus.Success.ToString())
            {
                // Qualified if the AI processed successfully; consultants can demote if needed
                newLead.TriageStatus = TriageStatus.Qualified;
            }
            else
            {
                // AI unavailable — flag for manual consultant review, not a silent Pending
                newLead.TriageStatus = TriageStatus.NeedsReview;
            }

            _context.Leads.Add(newLead);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeads), new { id = newLead.Id }, new
            {
                Message = "Lead successfully submitted.",
                LeadId = newLead.Id,
                TriageStatus = newLead.TriageStatus.ToString(),
                AiAvailable = aiResult.Status == RunStatus.Success.ToString(),
                AiAnalysis = aiResult.Status == RunStatus.Success.ToString() ? aiResult.ExtractedData : null,
                FallbackMessage = aiResult.Status != RunStatus.Success.ToString() ? aiResult.Message : null
            });
        }
    }
}
