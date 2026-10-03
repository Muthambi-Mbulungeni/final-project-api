using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/stats")]
    [Authorize]
    public class StatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// GET /api/stats/admin
        /// Aggregated platform metrics for the Admin dashboard.
        /// </summary>
        [HttpGet("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAdminStats()
        {
            var totalUsers    = await _context.Users.CountAsync(u => u.IsActive);
            var totalLeads    = await _context.Leads.CountAsync();
            var totalCases    = await _context.Cases.CountAsync(c => !c.IsArchived);
            var activeCases   = await _context.Cases.CountAsync(c => !c.IsArchived && c.Stage != CaseStage.FinalDecision);
            var qualifiedLeads   = await _context.Leads.CountAsync(l => l.TriageStatus == TriageStatus.Qualified);
            var unqualifiedLeads = await _context.Leads.CountAsync(l => l.TriageStatus == TriageStatus.Unqualified);
            var needsReviewLeads = await _context.Leads.CountAsync(l => l.TriageStatus == TriageStatus.NeedsReview);

            return Ok(new
            {
                TotalUsers       = totalUsers,
                TotalLeads       = totalLeads,
                TotalCases       = totalCases,
                ActiveCases      = activeCases,
                QualifiedLeads   = qualifiedLeads,
                UnqualifiedLeads = unqualifiedLeads,
                NeedsReviewLeads = needsReviewLeads
            });
        }

        /// <summary>
        /// GET /api/stats/consultant
        /// Metrics scoped to the current consultant's assigned cases and lead queue.
        /// </summary>
        [HttpGet("consultant")]
        [Authorize(Roles = "Admin,Consultant")]
        public async Task<IActionResult> GetConsultantStats()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(userIdStr, out var consultantId);

            var myCases      = await _context.Cases.CountAsync(c => c.ConsultantId == consultantId && !c.IsArchived);
            var activeCases  = await _context.Cases.CountAsync(c => c.ConsultantId == consultantId && !c.IsArchived && c.Stage != CaseStage.FinalDecision);
            var pendingLeads = await _context.Leads.CountAsync(l => l.TriageStatus == TriageStatus.Pending || l.TriageStatus == TriageStatus.NeedsReview);

            return Ok(new
            {
                MyCases      = myCases,
                ActiveCases  = activeCases,
                PendingLeads = pendingLeads
            });
        }

        /// <summary>
        /// GET /api/stats/client/{clientId}
        /// Metrics scoped to a specific client — current stage and document counts.
        /// Clients can only retrieve their own stats.
        /// </summary>
        [HttpGet("client/{clientId}")]
        [Authorize(Roles = "Admin,Consultant,Client")]
        public async Task<IActionResult> GetClientStats(Guid clientId)
        {
            // Clients may only view their own stats
            if (User.IsInRole("Client"))
            {
                var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!Guid.TryParse(userIdStr, out var currentUserId) || currentUserId != clientId)
                    return Forbid();
            }

            var activeCase = await _context.Cases
                .Where(c => c.ClientId == clientId && !c.IsArchived)
                .OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync();

            var totalDocuments = await _context.Documents
                .CountAsync(d => activeCase != null && d.CaseId == activeCase.Id);

            return Ok(new
            {
                HasActiveCase     = activeCase != null,
                CurrentStage      = activeCase?.Stage.ToString() ?? "None",
                TotalDocuments    = totalDocuments
            });
        }
    }
}
