using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Controllers;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.DTOs;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// POST /api/leads needs POPIA consent: without it the API answers 400 with the consent_required
    /// code before anything reaches the agent or the database; with it, the time of consent is stored.
    /// </summary>
    public class LeadConsentTests
    {
        private sealed class CountingAgent : IAgentService
        {
            public int Calls { get; private set; }

            public Task<AgentResponse> TriageLeadAsync(LeadTriageInput input, CancellationToken cancellationToken = default)
            {
                Calls++;
                return Task.FromResult(new AgentResponse
                {
                    Status = RunStatus.Success,
                    TriageStatus = TriageStatus.Qualified,
                    Output = "ok"
                });
            }
        }

        private static ApplicationDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

        private static CreateLeadRequest NewRequest(bool consent) => new()
        {
            FullName = "Kagiso Molefe",
            Email = "kagiso.molefe@example.com",
            ImmigrationStatus = ImmigrationStatus.Foreigner,
            NeedsAssistance = true,
            DesiredStartDate = DateTime.UtcNow.AddMonths(3),
            Consent = consent
        };

        [Fact]
        public async Task ConsentLeftOut_Returns400ConsentRequired_AndStoresNothing()
        {
            using var ctx = CreateContext();
            var agent = new CountingAgent();
            // A body that leaves "consent" out, as an older client or a hand-made request would.
            var dto = JsonSerializer.Deserialize<CreateLeadRequest>(
                "{\"fullName\":\"Kagiso Molefe\",\"email\":\"kagiso.molefe@example.com\",\"immigrationStatus\":3,\"desiredStartDate\":\"2027-01-15T00:00:00Z\"}")!;

            var result = await new LeadsController(ctx).CreateLead(dto, agent);

            AssertConsentRequired(result);
            Assert.Equal(0, agent.Calls);
            Assert.Empty(ctx.Leads);
            Assert.Empty(ctx.AgentRuns);
        }

        [Fact]
        public async Task ConsentFalse_Returns400ConsentRequired_AndStoresNothing()
        {
            using var ctx = CreateContext();
            var agent = new CountingAgent();

            var result = await new LeadsController(ctx).CreateLead(NewRequest(consent: false), agent);

            AssertConsentRequired(result);
            Assert.Equal(0, agent.Calls);
            Assert.Empty(ctx.Leads);
        }

        [Fact]
        public async Task ConsentTrue_CreatesTheLead_AndStoresWhenConsentWasGiven()
        {
            using var ctx = CreateContext();
            var agent = new CountingAgent();
            var before = DateTime.UtcNow;

            var result = await new LeadsController(ctx).CreateLead(NewRequest(consent: true), agent);

            Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(1, agent.Calls);
            var lead = Assert.Single(ctx.Leads);
            Assert.NotNull(lead.ConsentAt);
            Assert.InRange(lead.ConsentAt!.Value, before, DateTime.UtcNow);
        }

        private static void AssertConsentRequired(IActionResult result)
        {
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            var body = Assert.IsType<ApiErrorResponse>(bad.Value);
            Assert.Equal("consent_required", body.Code);
            Assert.Equal("You need to agree before we can assess your enquiry.", Assert.Single(body.Errors));
        }
    }
}
