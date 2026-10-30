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
    /// PATCH /api/cases/{id}/stage: ISS-07 (rejects archived cases and unchanged stage)
    /// and ISS-12 (audit log records sentence with old and new stage).
    /// </summary>
    public class CasesControllerStageTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static CasesController CreateController(ApplicationDbContext ctx, Guid? userId = null, string role = "Admin")
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            return new CasesController(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static async Task<Case> SeedCase(ApplicationDbContext ctx, CaseStage stage, bool isArchived = false, Guid? consultantId = null)
        {
            var c = new Case
            {
                Id = Guid.NewGuid(),
                ClientId = Guid.NewGuid(),
                ConsultantId = consultantId ?? Guid.NewGuid(),
                VisaCategoryId = Guid.NewGuid(),
                Stage = stage,
                IsArchived = isArchived
            };
            ctx.Cases.Add(c);
            await ctx.SaveChangesAsync();
            return c;
        }

        [Fact]
        public async Task UpdateStage_ArchivedCase_Returns400_AndWritesNoHistoryOrAudit()
        {
            using var ctx = CreateContext(nameof(UpdateStage_ArchivedCase_Returns400_AndWritesNoHistoryOrAudit));
            var c = await SeedCase(ctx, CaseStage.InitialIntake, isArchived: true);
            var controller = CreateController(ctx);

            var result = await controller.UpdateStage(c.Id, new UpdateCaseStageRequest { Stage = CaseStage.DocumentGathering });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var err = Assert.IsType<ApiErrorResponse>(badRequest.Value);
            Assert.Equal("This case is archived and cannot be changed.", err.Message);

            Assert.Empty(await ctx.CaseStageHistories.ToListAsync());
            Assert.Empty(await ctx.AuditLogs.ToListAsync());
            Assert.Equal(CaseStage.InitialIntake, (await ctx.Cases.FindAsync(c.Id))!.Stage);
        }

        [Fact]
        public async Task UpdateStage_SameStage_Returns400_AndWritesNoHistoryOrAudit()
        {
            using var ctx = CreateContext(nameof(UpdateStage_SameStage_Returns400_AndWritesNoHistoryOrAudit));
            var c = await SeedCase(ctx, CaseStage.DocumentGathering, isArchived: false);
            var controller = CreateController(ctx);

            var result = await controller.UpdateStage(c.Id, new UpdateCaseStageRequest { Stage = CaseStage.DocumentGathering });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var err = Assert.IsType<ApiErrorResponse>(badRequest.Value);
            Assert.Equal("The case is already at that stage.", err.Message);

            Assert.Empty(await ctx.CaseStageHistories.ToListAsync());
            Assert.Empty(await ctx.AuditLogs.ToListAsync());
            Assert.Equal(CaseStage.DocumentGathering, (await ctx.Cases.FindAsync(c.Id))!.Stage);
        }

        [Fact]
        public async Task UpdateStage_ValidNewStage_Returns200_WritesHistoryAndSentenceAuditLog()
        {
            using var ctx = CreateContext(nameof(UpdateStage_ValidNewStage_Returns200_WritesHistoryAndSentenceAuditLog));
            var adminId = Guid.NewGuid();
            var c = await SeedCase(ctx, CaseStage.InitialIntake, isArchived: false);
            var controller = CreateController(ctx, userId: adminId, role: "Admin");

            var result = await controller.UpdateStage(c.Id, new UpdateCaseStageRequest { Stage = CaseStage.DocumentGathering });

            Assert.IsType<OkObjectResult>(result);

            var updated = await ctx.Cases.FindAsync(c.Id);
            Assert.Equal(CaseStage.DocumentGathering, updated!.Stage);

            var history = Assert.Single(await ctx.CaseStageHistories.ToListAsync());
            Assert.Equal(c.Id, history.CaseId);
            Assert.Equal(CaseStage.InitialIntake, history.FromStage);
            Assert.Equal(CaseStage.DocumentGathering, history.ToStage);
            Assert.Equal(adminId, history.ChangedById);

            var audit = Assert.Single(await ctx.AuditLogs.ToListAsync());
            Assert.Equal(c.Id, audit.EntityId);
            Assert.Equal("Case", audit.EntityType);
            Assert.Equal(adminId, audit.UserId);
            Assert.Equal("Changed case stage from InitialIntake to DocumentGathering", audit.Action);
        }

        [Fact]
        public async Task UpdateStage_ConsultantOtherCase_Returns403()
        {
            using var ctx = CreateContext(nameof(UpdateStage_ConsultantOtherCase_Returns403));
            var ownerId = Guid.NewGuid();
            var otherConsultantId = Guid.NewGuid();
            var c = await SeedCase(ctx, CaseStage.InitialIntake, consultantId: ownerId);
            var controller = CreateController(ctx, userId: otherConsultantId, role: "Consultant");

            var result = await controller.UpdateStage(c.Id, new UpdateCaseStageRequest { Stage = CaseStage.DocumentGathering });

            Assert.IsType<ForbidResult>(result);
            Assert.Empty(await ctx.CaseStageHistories.ToListAsync());
            Assert.Empty(await ctx.AuditLogs.ToListAsync());
        }

        [Fact]
        public async Task UpdateStage_MissingStage_Returns400()
        {
            using var ctx = CreateContext(nameof(UpdateStage_MissingStage_Returns400));
            var c = await SeedCase(ctx, CaseStage.InitialIntake);
            var controller = CreateController(ctx);

            var result = await controller.UpdateStage(c.Id, new UpdateCaseStageRequest { Stage = null });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var err = Assert.IsType<ApiErrorResponse>(badRequest.Value);
            Assert.Equal("Stage is required.", err.Message);
        }
    }
}
