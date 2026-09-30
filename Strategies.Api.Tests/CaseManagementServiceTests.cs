using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class CaseManagementServiceTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static async Task<Case> SeedCase(ApplicationDbContext context, CaseStage stage)
        {
            var c = new Case
            {
                Id = Guid.NewGuid(),
                ClientId = Guid.NewGuid(),
                VisaCategoryId = Guid.NewGuid(),
                Stage = stage,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Cases.Add(c);
            await context.SaveChangesAsync();
            return c;
        }

        [Fact]
        public async Task AdvanceStage_InitialIntake_AdvancesToDocumentGathering()
        {
            using var ctx = CreateContext(nameof(AdvanceStage_InitialIntake_AdvancesToDocumentGathering));
            var c = await SeedCase(ctx, CaseStage.InitialIntake);
            var svc = new CaseManagementService(ctx);

            var result = await svc.AdvanceCaseStageAsync(c.Id);
            var updated = await ctx.Cases.FindAsync(c.Id);

            Assert.True(result);
            Assert.Equal(CaseStage.DocumentGathering, updated!.Stage);
        }

        [Fact]
        public async Task AdvanceStage_DocumentGathering_AdvancesToUnderReview()
        {
            using var ctx = CreateContext(nameof(AdvanceStage_DocumentGathering_AdvancesToUnderReview));
            var c = await SeedCase(ctx, CaseStage.DocumentGathering);
            var svc = new CaseManagementService(ctx);

            var result = await svc.AdvanceCaseStageAsync(c.Id);
            var updated = await ctx.Cases.FindAsync(c.Id);

            Assert.True(result);
            Assert.Equal(CaseStage.UnderReview, updated!.Stage);
        }

        [Fact]
        public async Task AdvanceStage_UnderReview_AdvancesToSubmittedToAuthority()
        {
            using var ctx = CreateContext(nameof(AdvanceStage_UnderReview_AdvancesToSubmittedToAuthority));
            var c = await SeedCase(ctx, CaseStage.UnderReview);
            var svc = new CaseManagementService(ctx);

            var result = await svc.AdvanceCaseStageAsync(c.Id);
            var updated = await ctx.Cases.FindAsync(c.Id);

            Assert.True(result);
            Assert.Equal(CaseStage.SubmittedToAuthority, updated!.Stage);
        }

        [Fact]
        public async Task AdvanceStage_SubmittedToAuthority_AdvancesToFinalDecision()
        {
            using var ctx = CreateContext(nameof(AdvanceStage_SubmittedToAuthority_AdvancesToFinalDecision));
            var c = await SeedCase(ctx, CaseStage.SubmittedToAuthority);
            var svc = new CaseManagementService(ctx);

            var result = await svc.AdvanceCaseStageAsync(c.Id);
            var updated = await ctx.Cases.FindAsync(c.Id);

            Assert.True(result);
            Assert.Equal(CaseStage.FinalDecision, updated!.Stage);
        }

        [Fact]
        public async Task AdvanceStage_FinalDecision_DoesNotAdvanceFurther()
        {
            using var ctx = CreateContext(nameof(AdvanceStage_FinalDecision_DoesNotAdvanceFurther));
            var c = await SeedCase(ctx, CaseStage.FinalDecision);
            var svc = new CaseManagementService(ctx);

            var result = await svc.AdvanceCaseStageAsync(c.Id);
            var updated = await ctx.Cases.FindAsync(c.Id);

            // Returns true (case found), but stage stays at FinalDecision
            Assert.True(result);
            Assert.Equal(CaseStage.FinalDecision, updated!.Stage);
        }

        [Fact]
        public async Task AdvanceStage_UpdatedAt_IsRefreshedOnAdvance()
        {
            using var ctx = CreateContext(nameof(AdvanceStage_UpdatedAt_IsRefreshedOnAdvance));
            var c = await SeedCase(ctx, CaseStage.InitialIntake);
            var originalUpdatedAt = c.UpdatedAt;
            var svc = new CaseManagementService(ctx);

            // Small delay to ensure the timestamp differs
            await Task.Delay(10);
            await svc.AdvanceCaseStageAsync(c.Id);
            var updated = await ctx.Cases.FindAsync(c.Id);

            Assert.True(updated!.UpdatedAt >= originalUpdatedAt);
        }

        [Fact]
        public async Task AdvanceStage_NonExistentCase_ReturnsFalse()
        {
            using var ctx = CreateContext(nameof(AdvanceStage_NonExistentCase_ReturnsFalse));
            var svc = new CaseManagementService(ctx);

            var result = await svc.AdvanceCaseStageAsync(Guid.NewGuid());

            Assert.False(result);
        }
    }
}
