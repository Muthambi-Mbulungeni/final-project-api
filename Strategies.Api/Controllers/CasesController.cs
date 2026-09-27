using Microsoft.AspNetCore.Mvc;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using System;
using System.Collections.Generic;

namespace Strategies.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CasesController : ControllerBase
    {
        // GET: api/cases/{clientId}
        // Unblocks Kundai: Kotlin Mobile App Case Status Screen
        [HttpGet("client/{clientId}")]
        public IActionResult GetClientCases(Guid clientId)
        {
            var mockCase = new Case
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                VisaCategoryId = Guid.NewGuid(),
                Stage = CaseStage.DocumentGathering,
                CreatedAt = DateTime.UtcNow.AddDays(-14),
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(new List<Case> { mockCase });
        }

        // GET: api/cases
        // Unblocks Riaad: Consultant Active Cases Dashboard
        [HttpGet]
        public IActionResult GetAllCases()
        {
            var mockCases = new List<Case>
            {
                new Case
                {
                    Id = Guid.NewGuid(),
                    ClientId = Guid.NewGuid(),
                    Stage = CaseStage.UnderReview,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new Case
                {
                    Id = Guid.NewGuid(),
                    ClientId = Guid.NewGuid(),
                    Stage = CaseStage.SubmittedToAuthority,
                    CreatedAt = DateTime.UtcNow.AddDays(-60)
                }
            };

            return Ok(mockCases);
        }
    }
}