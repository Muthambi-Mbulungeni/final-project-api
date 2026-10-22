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
    /// GET /api/cases: a consultant's list leaves out archived cases; an admin's list keeps them.
    /// </summary>
    public class CaseListTests
    {
        private static readonly Guid ConsultantId = Guid.NewGuid();

        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static CasesController CreateController(ApplicationDbContext ctx, Guid userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
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

        private static Case NewCase(Guid? consultantId, CaseStage stage, bool archived) => new Case
        {
            Id = Guid.NewGuid(),
            ClientId = Guid.NewGuid(),
            VisaCategoryId = Guid.NewGuid(),
            ConsultantId = consultantId,
            Stage = stage,
            IsArchived = archived
        };

        // Own active case, own finished case, own archived case, and another consultant's case.
        private static async Task<(Case active, Case finished, Case archived, Case other)> Seed(ApplicationDbContext ctx)
        {
            var active = NewCase(ConsultantId, CaseStage.UnderReview, archived: false);
            var finished = NewCase(ConsultantId, CaseStage.FinalDecision, archived: false);
            var archived = NewCase(ConsultantId, CaseStage.DocumentGathering, archived: true);
            var other = NewCase(Guid.NewGuid(), CaseStage.InitialIntake, archived: false);
            ctx.Cases.AddRange(active, finished, archived, other);
            await ctx.SaveChangesAsync();
            return (active, finished, archived, other);
        }

        private static PagedResponse<CaseDto> Page(IActionResult result)
        {
            var ok = Assert.IsType<OkObjectResult>(result);
            return Assert.IsType<PagedResponse<CaseDto>>(ok.Value);
        }

        [Fact]
        public async Task Consultant_SeesOwnCasesWithoutArchived()
        {
            using var ctx = CreateContext(nameof(Consultant_SeesOwnCasesWithoutArchived));
            var (active, finished, _, _) = await Seed(ctx);

            var page = Page(await CreateController(ctx, ConsultantId, "Consultant").GetCases());

            Assert.Equal(new[] { active.Id, finished.Id }.OrderBy(id => id), page.Data.Select(c => c.Id).OrderBy(id => id));
            Assert.Equal(2, page.TotalRecords);
            Assert.Equal(1, page.TotalPages);
        }

        [Fact]
        public async Task Consultant_StageFilterNeverReturnsArchived()
        {
            using var ctx = CreateContext(nameof(Consultant_StageFilterNeverReturnsArchived));
            await Seed(ctx);

            var page = Page(await CreateController(ctx, ConsultantId, "Consultant").GetCases(stage: "DocumentGathering"));

            Assert.Empty(page.Data);
            Assert.Equal(0, page.TotalRecords);
        }

        [Fact]
        public async Task Admin_SeesEveryCaseIncludingArchived()
        {
            using var ctx = CreateContext(nameof(Admin_SeesEveryCaseIncludingArchived));
            var (_, _, archived, _) = await Seed(ctx);

            var page = Page(await CreateController(ctx, Guid.NewGuid(), "Admin").GetCases());

            Assert.Equal(4, page.TotalRecords);
            Assert.Contains(page.Data, c => c.Id == archived.Id && c.IsArchived);
        }
    }
}
