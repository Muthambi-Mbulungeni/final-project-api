using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Controllers;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// PATCH /api/leads/{id}/status: ISS-12 (rejects unchanged status, records sentence with old and new verdicts in audit log).
    /// </summary>
    public class LeadsStatusTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static LeadsController CreateController(ApplicationDbContext ctx, Guid? userId = null, string role = "Consultant")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            return new LeadsController(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static async Task<Lead> SeedLead(ApplicationDbContext ctx, TriageStatus status = TriageStatus.Pending)
        {
            var lead = new Lead
            {
                Id = Guid.NewGuid(),
                FullName = "Applicant One",
                Email = "applicant@example.com",
                ImmigrationStatus = ImmigrationStatus.Citizen,
                NeedsAssistance = true,
                DesiredStartDate = DateTime.UtcNow.AddMonths(1),
                TriageStatus = status,
                ConsentAt = DateTime.UtcNow,
                SubmittedAt = DateTime.UtcNow
            };
            ctx.Leads.Add(lead);
            await ctx.SaveChangesAsync();
            return lead;
        }

        [Fact]
        public async Task UpdateLeadStatus_MissingStatus_Returns400()
        {
            using var ctx = CreateContext(nameof(UpdateLeadStatus_MissingStatus_Returns400));
            var lead = await SeedLead(ctx);
            var controller = CreateController(ctx);

            var result = await controller.UpdateLeadStatus(lead.Id, new UpdateLeadStatusRequest { Status = null });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var err = Assert.IsType<ApiErrorResponse>(badRequest.Value);
            Assert.Equal("Status is required.", err.Message);
        }

        [Fact]
        public async Task UpdateLeadStatus_NonexistentLead_Returns404()
        {
            using var ctx = CreateContext(nameof(UpdateLeadStatus_NonexistentLead_Returns404));
            var controller = CreateController(ctx);

            var result = await controller.UpdateLeadStatus(Guid.NewGuid(), new UpdateLeadStatusRequest { Status = TriageStatus.Qualified });

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            var err = Assert.IsType<ApiErrorResponse>(notFound.Value);
            Assert.Equal("Lead not found.", err.Message);
        }

        [Fact]
        public async Task UpdateLeadStatus_SameStatus_Returns400_AndWritesNoAudit()
        {
            using var ctx = CreateContext(nameof(UpdateLeadStatus_SameStatus_Returns400_AndWritesNoAudit));
            var lead = await SeedLead(ctx, TriageStatus.Qualified);
            var controller = CreateController(ctx);

            var result = await controller.UpdateLeadStatus(lead.Id, new UpdateLeadStatusRequest { Status = TriageStatus.Qualified });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var err = Assert.IsType<ApiErrorResponse>(badRequest.Value);
            Assert.Equal("The lead already has that verdict.", err.Message);

            Assert.Empty(await ctx.AuditLogs.ToListAsync());
            Assert.Equal(TriageStatus.Qualified, (await ctx.Leads.FindAsync(lead.Id))!.TriageStatus);
        }

        [Fact]
        public async Task UpdateLeadStatus_NewStatus_Returns200_UpdatesStatus_AndWritesSentenceAudit()
        {
            using var ctx = CreateContext(nameof(UpdateLeadStatus_NewStatus_Returns200_UpdatesStatus_AndWritesSentenceAudit));
            var reviewerId = Guid.NewGuid();
            var lead = await SeedLead(ctx, TriageStatus.Pending);
            var controller = CreateController(ctx, userId: reviewerId, role: "Consultant");

            var result = await controller.UpdateLeadStatus(lead.Id, new UpdateLeadStatusRequest { Status = TriageStatus.Qualified });

            Assert.IsType<OkObjectResult>(result);

            var updated = await ctx.Leads.FindAsync(lead.Id);
            Assert.Equal(TriageStatus.Qualified, updated!.TriageStatus);
            Assert.Equal(reviewerId, updated.ReviewedById);

            var audit = Assert.Single(await ctx.AuditLogs.ToListAsync());
            Assert.Equal(lead.Id, audit.EntityId);
            Assert.Equal("Lead", audit.EntityType);
            Assert.Equal(reviewerId, audit.UserId);
            Assert.Equal("Changed lead verdict from Pending to Qualified", audit.Action);
        }
    }
}
