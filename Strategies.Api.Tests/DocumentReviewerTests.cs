using System;
using System.Collections;
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
    /// Approve and reject record who decided and when; GET /api/documents and GET /api/documents/{id}
    /// return the reviewer's name and the date, also for an admin and for a deleted reviewer.
    /// </summary>
    public class DocumentReviewerTests
    {
        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static DocumentsController CreateController(ApplicationDbContext ctx, Guid userId, string role)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }, "Test");

            return new DocumentsController(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private sealed record Seeded(Case Case, Document Document, User Client, User Consultant, User Admin);

        private static async Task<Seeded> Seed(ApplicationDbContext ctx)
        {
            var client = new User { Id = Guid.NewGuid(), FullName = "Lerato Mahlangu", Email = "client@example.com", Role = UserRole.Client };
            var consultant = new User { Id = Guid.NewGuid(), FullName = "Nomvula Zulu", Email = "consultant@example.com", Role = UserRole.Consultant };
            var admin = new User { Id = Guid.NewGuid(), FullName = "System Admin", Email = "admin@example.com", Role = UserRole.Admin };
            var type = new DocumentType { Id = Guid.NewGuid(), Name = "Medical report" };
            var c = new Case
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                ConsultantId = consultant.Id,
                VisaCategoryId = Guid.NewGuid(),
                Stage = CaseStage.DocumentGathering
            };
            var doc = new Document
            {
                Id = Guid.NewGuid(),
                CaseId = c.Id,
                DocumentTypeId = type.Id,
                FileName = "medical-report.pdf",
                FilePath = "uploads/medical-report.pdf",
                Status = DocumentStatus.Pending
            };
            ctx.Users.AddRange(client, consultant, admin);
            ctx.DocumentTypes.Add(type);
            ctx.Cases.Add(c);
            ctx.Documents.Add(doc);
            await ctx.SaveChangesAsync();
            return new Seeded(c, doc, client, consultant, admin);
        }

        private static object? Prop(object item, string name) => item.GetType().GetProperty(name)!.GetValue(item);

        // GET /api/documents returns an anonymous page object; its Data holds the rows.
        private static object OnlyRow(IActionResult result)
        {
            var ok = Assert.IsType<OkObjectResult>(result);
            var rows = Assert.IsAssignableFrom<IEnumerable>(Prop(ok.Value!, "Data")).Cast<object>().ToList();
            return Assert.Single(rows);
        }

        private static Task<IActionResult> Review(ApplicationDbContext ctx, User reviewer, string role, Guid documentId, string status, string? comment = null) =>
            CreateController(ctx, reviewer.Id, role).UpdateDocumentStatus(documentId, new UpdateDocumentStatusRequest { Status = status, ReviewComment = comment });

        [Fact]
        public async Task Approve_ByConsultant_RecordsReviewerAndTime()
        {
            using var ctx = CreateContext(nameof(Approve_ByConsultant_RecordsReviewerAndTime));
            var s = await Seed(ctx);
            var before = DateTime.UtcNow;

            Assert.IsType<OkObjectResult>(await Review(ctx, s.Consultant, "Consultant", s.Document.Id, "Approved"));

            var saved = await ctx.Documents.FindAsync(s.Document.Id);
            Assert.Equal(s.Consultant.Id, saved!.ReviewedById);
            Assert.NotNull(saved.ReviewedAt);
            Assert.InRange(saved.ReviewedAt!.Value, before, DateTime.UtcNow);
        }

        [Fact]
        public async Task Reject_ByAdmin_RecordsTheAdmin()
        {
            using var ctx = CreateContext(nameof(Reject_ByAdmin_RecordsTheAdmin));
            var s = await Seed(ctx);

            Assert.IsType<OkObjectResult>(await Review(ctx, s.Admin, "Admin", s.Document.Id, "Rejected", "Please upload a recent report."));

            var saved = await ctx.Documents.FindAsync(s.Document.Id);
            Assert.Equal(s.Admin.Id, saved!.ReviewedById);
            Assert.NotNull(saved.ReviewedAt);
        }

        [Fact]
        public async Task BackToPending_ClearsReviewer()
        {
            using var ctx = CreateContext(nameof(BackToPending_ClearsReviewer));
            var s = await Seed(ctx);
            await Review(ctx, s.Consultant, "Consultant", s.Document.Id, "Approved");

            await Review(ctx, s.Consultant, "Consultant", s.Document.Id, "Pending");

            var saved = await ctx.Documents.FindAsync(s.Document.Id);
            Assert.Null(saved!.ReviewedById);
            Assert.Null(saved.ReviewedAt);
        }

        [Fact]
        public async Task List_ForClient_ReturnsConsultantNameAndDate()
        {
            using var ctx = CreateContext(nameof(List_ForClient_ReturnsConsultantNameAndDate));
            var s = await Seed(ctx);
            await Review(ctx, s.Consultant, "Consultant", s.Document.Id, "Rejected", "Too old.");

            var row = OnlyRow(await CreateController(ctx, s.Client.Id, "Client").GetDocuments(s.Case.Id));

            Assert.Equal("Nomvula Zulu", Prop(row, "ReviewedByName"));
            Assert.NotNull(Prop(row, "ReviewedAt"));
            Assert.Equal("Too old.", Prop(row, "ReviewComment"));
        }

        [Fact]
        public async Task List_AdminReviewer_ReturnsAdminName()
        {
            using var ctx = CreateContext(nameof(List_AdminReviewer_ReturnsAdminName));
            var s = await Seed(ctx);
            await Review(ctx, s.Admin, "Admin", s.Document.Id, "Approved");

            var row = OnlyRow(await CreateController(ctx, s.Consultant.Id, "Consultant").GetDocuments(s.Case.Id));

            Assert.Equal("System Admin", Prop(row, "ReviewedByName"));
        }

        [Fact]
        public async Task List_DeletedReviewer_StillReturnsName()
        {
            using var ctx = CreateContext(nameof(List_DeletedReviewer_StillReturnsName));
            var s = await Seed(ctx);
            await Review(ctx, s.Consultant, "Consultant", s.Document.Id, "Approved");
            s.Consultant.DeletedAt = DateTime.UtcNow;
            s.Consultant.IsActive = false;
            await ctx.SaveChangesAsync();

            var row = OnlyRow(await CreateController(ctx, s.Client.Id, "Client").GetDocuments(s.Case.Id));

            Assert.Equal("Nomvula Zulu", Prop(row, "ReviewedByName"));
        }

        [Fact]
        public async Task List_NotReviewed_ReturnsNoReviewer()
        {
            using var ctx = CreateContext(nameof(List_NotReviewed_ReturnsNoReviewer));
            var s = await Seed(ctx);

            var row = OnlyRow(await CreateController(ctx, s.Client.Id, "Client").GetDocuments(s.Case.Id));

            Assert.Null(Prop(row, "ReviewedByName"));
            Assert.Null(Prop(row, "ReviewedAt"));
        }

        [Fact]
        public async Task Single_ReturnsReviewerNameAndDate()
        {
            using var ctx = CreateContext(nameof(Single_ReturnsReviewerNameAndDate));
            var s = await Seed(ctx);
            await Review(ctx, s.Consultant, "Consultant", s.Document.Id, "Approved");

            var ok = Assert.IsType<OkObjectResult>(await CreateController(ctx, s.Client.Id, "Client").GetDocument(s.Document.Id));

            Assert.Equal("Nomvula Zulu", Prop(ok.Value!, "ReviewedByName"));
            Assert.NotNull(Prop(ok.Value!, "ReviewedAt"));
        }
    }
}
