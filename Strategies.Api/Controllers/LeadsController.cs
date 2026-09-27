using Microsoft.AspNetCore.Mvc;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using System;
using System.Collections.Generic;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LeadsController : ControllerBase
    {
        // GET: api/leads
        // Unblocks Riaad: Consultant Dashboard Lead View
        [HttpGet]
        public IActionResult GetLeads()
        {
            var mockLeads = new List<Lead>
            {
                new Lead
                {
                    Id = Guid.NewGuid(),
                    FullName = "John Doe",
                    Email = "john.doe@example.com",
                    ImmigrationStatus = ImmigrationStatus.Foreigner,
                    TriageStatus = TriageStatus.Pending,
                    DesiredStartDate = DateTime.UtcNow.AddMonths(2),
                    SubmittedAt = DateTime.UtcNow
                },
                new Lead
                {
                    Id = Guid.NewGuid(),
                    FullName = "Jane Smith",
                    Email = "jane.smith@example.com",
                    ImmigrationStatus = ImmigrationStatus.TemporaryResident,
                    TriageStatus = TriageStatus.Qualified,
                    DesiredStartDate = DateTime.UtcNow.AddMonths(1),
                    SubmittedAt = DateTime.UtcNow.AddDays(-2)
                }
            };

            return Ok(mockLeads);
        }

        // POST: api/leads
        // Unblocks Riaad: Public Intake Form Submission
        [HttpPost]
        public IActionResult CreateLead([FromBody] Lead newLead)
        {
            // Simulate a successful save and return a 201 Created status
            // In a real scenario, the AI triage agent would run here.
            return CreatedAtAction(nameof(GetLeads), new { id = Guid.NewGuid() }, new
            {
                Message = "Lead successfully submitted.",
                LeadId = Guid.NewGuid(),
                Status = TriageStatus.Pending.ToString()
            });
        }
    }
}