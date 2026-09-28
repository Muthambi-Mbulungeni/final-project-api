using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.Services;
using System;
using System.Threading.Tasks;

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

        [HttpGet]
        public async Task<IActionResult> GetLeads()
        {
            var leads = await _context.Leads.ToListAsync();
            return Ok(leads);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] Lead newLead, [FromServices] IAgentService agentService)
        {
            var aiResult = await agentService.ProcessLeadAsync($"Name: {newLead.FullName}, Status: {newLead.ImmigrationStatus}");

            newLead.Id = Guid.NewGuid();
            newLead.TriageStatus = aiResult.Status == RunStatus.Success.ToString() ? TriageStatus.Qualified : TriageStatus.Pending;
            newLead.SubmittedAt = DateTime.UtcNow;

            _context.Leads.Add(newLead);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLeads), new { id = newLead.Id }, new
            {
                Message = "Lead successfully submitted.",
                LeadId = newLead.Id,
                AiAnalysis = aiResult.ExtractedData,
                Status = newLead.TriageStatus.ToString()
            });
        }
    }
}