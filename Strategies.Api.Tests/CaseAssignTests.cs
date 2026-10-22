using System;
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
    /// PATCH /api/cases/{id}/assign only accepts an active, not deleted consultant.
    /// </summary>
    public class CaseAssignTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static CasesController CreateController(ApplicationDbContext ctx)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test");

            return new CasesController(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static async Task<(Case c, User assignee)> Seed(
            ApplicationDbContext ctx, UserRole role, bool isActive = true, DateTime? deletedAt = null)
        {
            var assignee = new User
            {
                Id = Guid.NewGuid(),
                FullName = "Assignee",
                Email = "assignee@example.com",
                Role = role,
                IsActive = isActive,
                DeletedAt = deletedAt
            };
            var c = new Case
            {
                Id = Guid.NewGuid(),
                ClientId = Guid.NewGuid(),
                VisaCategoryId = Guid.NewGuid(),
                Stage = CaseStage.InitialIntake
            };
            ctx.Users.Add(assignee);
            ctx.Cases.Add(c);
            await ctx.SaveChangesAsync();
            return (c, assignee);
        }

        [Fact]
        public async Task Assign_ActiveConsultant_Returns200AndSetsConsultant()
        {
            using var ctx = CreateContext(nameof(Assign_ActiveConsultant_Returns200AndSetsConsultant));
            var (c, consultant) = await Seed(ctx, UserRole.Consultant);

            var result = await CreateController(ctx).AssignConsultant(c.Id, new AssignConsultantRequest { ConsultantId = consultant.Id });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(consultant.Id, (await ctx.Cases.FindAsync(c.Id))!.ConsultantId);
        }

        [Theory]
        [InlineData(UserRole.Client, true, false)]
        [InlineData(UserRole.Admin, true, false)]
        [InlineData(UserRole.Consultant, false, false)]
        [InlineData(UserRole.Consultant, true, true)]
        public async Task Assign_NotAnActiveConsultant_Returns400AndLeavesCase(UserRole role, bool isActive, bool deleted)
        {
            using var ctx = CreateContext($"{nameof(Assign_NotAnActiveConsultant_Returns400AndLeavesCase)}{role}{isActive}{deleted}");
            var (c, assignee) = await Seed(ctx, role, isActive, deleted ? DateTime.UtcNow : null);

            var result = await CreateController(ctx).AssignConsultant(c.Id, new AssignConsultantRequest { ConsultantId = assignee.Id });

            var body = Assert.IsType<ApiErrorResponse>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.Contains("Choose an active consultant.", body.Errors);
            Assert.Null((await ctx.Cases.FindAsync(c.Id))!.ConsultantId);
            Assert.Empty(await ctx.AuditLogs.ToListAsync());
        }

        [Fact]
        public async Task Assign_UnknownConsultant_Returns400()
        {
            using var ctx = CreateContext(nameof(Assign_UnknownConsultant_Returns400));
            var (c, _) = await Seed(ctx, UserRole.Consultant);

            var result = await CreateController(ctx).AssignConsultant(c.Id, new AssignConsultantRequest { ConsultantId = Guid.NewGuid() });

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
