using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Strategies.Api.Controllers;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;
using Xunit;

namespace Strategies.Api.Tests
{
    public class MessagesControllerTests
    {
        private static readonly Guid AdminId = Guid.NewGuid();
        private static readonly Guid ConsultantId = Guid.NewGuid();
        private static readonly Guid OtherConsultantId = Guid.NewGuid();
        private static readonly Guid ClientId = Guid.NewGuid();
        private static readonly Guid OtherClientId = Guid.NewGuid();
        private static readonly Guid CaseId = Guid.NewGuid();
        private static readonly Guid OtherCaseId = Guid.NewGuid();
        private static readonly Guid ArchivedCaseId = Guid.NewGuid();

        // CaseId: ClientId's case, assigned to ConsultantId. OtherCaseId: OtherClientId's, assigned
        // to OtherConsultantId. ArchivedCaseId: ClientId's, archived.
        private static async Task<ApplicationDbContext> CreateContextAsync(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            var context = new ApplicationDbContext(options);

            context.Users.AddRange(
                new User { Id = AdminId, FullName = "System Admin", Email = "admin@test.com", Role = UserRole.Admin },
                new User { Id = ConsultantId, FullName = "Consultant User", Email = "consultant@test.com", Role = UserRole.Consultant },
                new User { Id = OtherConsultantId, FullName = "Other Consultant", Email = "other.consultant@test.com", Role = UserRole.Consultant },
                new User { Id = ClientId, FullName = "John Doe", Email = "client@test.com", Role = UserRole.Client },
                new User { Id = OtherClientId, FullName = "Ayesha Patel", Email = "other.client@test.com", Role = UserRole.Client });
            context.Cases.AddRange(
                new Case { Id = CaseId, ClientId = ClientId, ConsultantId = ConsultantId, VisaCategoryId = Guid.NewGuid() },
                new Case { Id = OtherCaseId, ClientId = OtherClientId, ConsultantId = OtherConsultantId, VisaCategoryId = Guid.NewGuid() },
                new Case { Id = ArchivedCaseId, ClientId = ClientId, ConsultantId = ConsultantId, VisaCategoryId = Guid.NewGuid(), IsArchived = true });
            await context.SaveChangesAsync();
            return context;
        }

        private static MessagesController CreateController(ApplicationDbContext context, Guid userId, string role) => new(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                        new Claim(ClaimTypes.Role, role)
                    }, "Test"))
                }
            }
        };

        private static Message AddMessage(ApplicationDbContext context, Guid caseId, Guid senderId, string body, DateTime sentAt)
        {
            var message = new Message { Id = Guid.NewGuid(), CaseId = caseId, SenderId = senderId, Body = body, SentAt = sentAt };
            context.Messages.Add(message);
            return message;
        }

        private static List<MessageDto> Page(IActionResult result) =>
            Assert.IsType<PagedResponse<MessageDto>>(Assert.IsType<OkObjectResult>(result).Value).Data.ToList();

        private static void AssertBodyError(IActionResult result, string expectedError)
        {
            var error = Assert.IsType<ApiErrorResponse>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.Equal("Validation failed.", error.Message);
            Assert.Equal(new[] { expectedError }, error.Errors);
        }

        // ── Access ──────────────────────────────────────────────────────────

        [Fact]
        public async Task Client_CanReadAndPostOnOwnCase()
        {
            using var context = await CreateContextAsync(nameof(Client_CanReadAndPostOnOwnCase));
            var controller = CreateController(context, ClientId, "Client");

            var post = await controller.SendMessage(CaseId, new SendMessageRequest { Body = "  Hello, I uploaded my passport.  " });
            var created = Assert.IsType<MessageDto>(Assert.IsType<CreatedAtActionResult>(post).Value);
            var thread = Page(await controller.GetMessages(CaseId));

            Assert.Equal("Hello, I uploaded my passport.", created.Body);
            Assert.Equal(ClientId, created.SenderId);
            Assert.Equal("John Doe", created.SenderName);
            Assert.Equal("Client", created.SenderRole);
            Assert.False(created.IsRead);
            Assert.Equal(created.Id, Assert.Single(thread).Id);
        }

        [Fact]
        public async Task Client_OtherClientsCase_Returns403()
        {
            using var context = await CreateContextAsync(nameof(Client_OtherClientsCase_Returns403));
            var controller = CreateController(context, ClientId, "Client");

            Assert.IsType<ForbidResult>(await controller.GetMessages(OtherCaseId));
            Assert.IsType<ForbidResult>(await controller.SendMessage(OtherCaseId, new SendMessageRequest { Body = "Hi" }));
            Assert.IsType<ForbidResult>(await controller.MarkThreadRead(OtherCaseId));
            Assert.Empty(context.Messages);
        }

        [Fact]
        public async Task Consultant_CanReadAndPostOnAssignedCase()
        {
            using var context = await CreateContextAsync(nameof(Consultant_CanReadAndPostOnAssignedCase));
            var controller = CreateController(context, ConsultantId, "Consultant");

            var post = await controller.SendMessage(CaseId, new SendMessageRequest { Body = "Please upload a recent medical report." });
            var created = Assert.IsType<MessageDto>(Assert.IsType<CreatedAtActionResult>(post).Value);

            Assert.Equal("Consultant", created.SenderRole);
            Assert.Single(Page(await controller.GetMessages(CaseId)));
        }

        [Fact]
        public async Task Consultant_UnassignedCase_Returns403()
        {
            using var context = await CreateContextAsync(nameof(Consultant_UnassignedCase_Returns403));
            var controller = CreateController(context, ConsultantId, "Consultant");

            Assert.IsType<ForbidResult>(await controller.GetMessages(OtherCaseId));
            Assert.IsType<ForbidResult>(await controller.SendMessage(OtherCaseId, new SendMessageRequest { Body = "Hi" }));
            Assert.IsType<ForbidResult>(await controller.MarkThreadRead(OtherCaseId));
            Assert.Empty(context.Messages);
        }

        [Fact]
        public async Task Admin_CanReadAndPostOnAnyCase()
        {
            using var context = await CreateContextAsync(nameof(Admin_CanReadAndPostOnAnyCase));
            var controller = CreateController(context, AdminId, "Admin");

            foreach (var caseId in new[] { CaseId, OtherCaseId })
            {
                var post = await controller.SendMessage(caseId, new SendMessageRequest { Body = "Admin note." });
                Assert.Equal("Admin", Assert.IsType<MessageDto>(Assert.IsType<CreatedAtActionResult>(post).Value).SenderRole);
                Assert.Single(Page(await controller.GetMessages(caseId)));
            }
        }

        [Fact]
        public async Task Admin_MarkRead_Returns403AndChangesNothing()
        {
            using var context = await CreateContextAsync(nameof(Admin_MarkRead_Returns403AndChangesNothing));
            AddMessage(context, CaseId, ClientId, "Unread for staff", DateTime.UtcNow);
            await context.SaveChangesAsync();

            var result = await CreateController(context, AdminId, "Admin").MarkThreadRead(CaseId);

            Assert.IsType<ForbidResult>(result);
            Assert.False(Assert.Single(context.Messages).IsRead);
        }

        [Fact]
        public async Task UnknownCase_Returns404ForEveryRole()
        {
            using var context = await CreateContextAsync(nameof(UnknownCase_Returns404ForEveryRole));
            var unknown = Guid.NewGuid();

            foreach (var (userId, role) in new[] { (AdminId, "Admin"), (ConsultantId, "Consultant"), (ClientId, "Client") })
            {
                var controller = CreateController(context, userId, role);
                foreach (var result in new[]
                {
                    await controller.GetMessages(unknown),
                    await controller.SendMessage(unknown, new SendMessageRequest { Body = "Hi" }),
                    await controller.MarkThreadRead(unknown)
                })
                {
                    var notFound = Assert.IsType<NotFoundObjectResult>(result);
                    Assert.Equal("Case not found.", Assert.IsType<ApiErrorResponse>(notFound.Value).Message);
                }
            }
        }

        // ── Body validation ────────────────────────────────────────────────

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   \t\n  ")]
        public async Task Post_EmptyOrWhitespaceBody_Returns400WithErrors(string? body)
        {
            using var context = await CreateContextAsync(nameof(Post_EmptyOrWhitespaceBody_Returns400WithErrors) + body?.Length);

            var result = await CreateController(context, ClientId, "Client").SendMessage(CaseId, new SendMessageRequest { Body = body });

            AssertBodyError(result, MessagesController.BodyRequiredError);
            Assert.Empty(context.Messages);
        }

        [Fact]
        public async Task Post_2001Characters_Returns400WithErrors()
        {
            using var context = await CreateContextAsync(nameof(Post_2001Characters_Returns400WithErrors));

            var result = await CreateController(context, ClientId, "Client")
                .SendMessage(CaseId, new SendMessageRequest { Body = new string('a', 2001) });

            AssertBodyError(result, MessagesController.BodyTooLongError);
            Assert.Empty(context.Messages);
        }

        [Fact]
        public async Task Post_Exactly2000CharactersAfterTrim_IsAccepted()
        {
            using var context = await CreateContextAsync(nameof(Post_Exactly2000CharactersAfterTrim_IsAccepted));

            var result = await CreateController(context, ClientId, "Client")
                .SendMessage(CaseId, new SendMessageRequest { Body = "  " + new string('a', 2000) + "  " });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Post_2000EmojiCountedAsCodePoints_IsAccepted()
        {
            // 2000 code points but 4000 UTF-16 units: the web counts code points, so the API must too.
            using var context = await CreateContextAsync(nameof(Post_2000EmojiCountedAsCodePoints_IsAccepted));
            var body = string.Concat(Enumerable.Repeat("😀", 2000));
            Assert.Equal(4000, body.Length);

            var result = await CreateController(context, ClientId, "Client").SendMessage(CaseId, new SendMessageRequest { Body = body });

            Assert.IsType<CreatedAtActionResult>(result);
        }

        [Fact]
        public async Task Post_ArchivedCase_Returns400WithoutErrors()
        {
            using var context = await CreateContextAsync(nameof(Post_ArchivedCase_Returns400WithoutErrors));

            var result = await CreateController(context, ClientId, "Client")
                .SendMessage(ArchivedCaseId, new SendMessageRequest { Body = "Hello?" });

            var error = Assert.IsType<ApiErrorResponse>(Assert.IsType<BadRequestObjectResult>(result).Value);
            Assert.Equal("Messages cannot be sent on an archived case.", error.Message);
            // An empty errors list is how the web tells "case closed" from "fix your text".
            Assert.Empty(error.Errors);
            Assert.Empty(context.Messages);
        }

        [Fact]
        public async Task Post_WritesAuditRow()
        {
            using var context = await CreateContextAsync(nameof(Post_WritesAuditRow));

            var post = await CreateController(context, ConsultantId, "Consultant").SendMessage(CaseId, new SendMessageRequest { Body = "Noted." });
            var created = Assert.IsType<MessageDto>(Assert.IsType<CreatedAtActionResult>(post).Value);

            var audit = Assert.Single(context.AuditLogs);
            Assert.Equal("Sent message", audit.Action);
            Assert.Equal("Message", audit.EntityType);
            Assert.Equal(created.Id, audit.EntityId);
            Assert.Equal(ConsultantId, audit.UserId);
        }

        [Fact]
        public void BodyBindingFilter_InvalidModelState_ReturnsApiErrorShapeBeforeAutomatic400()
        {
            var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
            actionContext.ModelState.AddModelError("request", "A non-empty request body is required.");
            var context = new ActionExecutingContext(actionContext, new List<IFilterMetadata>(), new Dictionary<string, object?>(), controller: null!);
            var filter = new MessageBodyBindingFilterAttribute();

            filter.OnActionExecuting(context);

            AssertBodyError(Assert.IsAssignableFrom<IActionResult>(context.Result), MessagesController.BodyRequiredError);
            var automatic = new ModelStateInvalidFilter(new ApiBehaviorOptions { InvalidModelStateResponseFactory = _ => null! }, NullLogger.Instance);
            Assert.True(filter.Order < automatic.Order);
        }

        // ── Mark read ──────────────────────────────────────────────────────

        [Fact]
        public async Task MarkRead_OnlyChangesMessagesFromOtherParties()
        {
            using var context = await CreateContextAsync(nameof(MarkRead_OnlyChangesMessagesFromOtherParties));
            var now = DateTime.UtcNow;
            var fromClient = AddMessage(context, CaseId, ClientId, "From client", now.AddMinutes(-3));
            var fromConsultant = AddMessage(context, CaseId, ConsultantId, "From consultant", now.AddMinutes(-2));
            var fromAdmin = AddMessage(context, CaseId, AdminId, "From admin", now.AddMinutes(-1));
            var otherCase = AddMessage(context, OtherCaseId, OtherConsultantId, "Other case", now);
            await context.SaveChangesAsync();

            var result = await CreateController(context, ClientId, "Client").MarkThreadRead(CaseId);

            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(2, (int)ok.Value!.GetType().GetProperty("Updated")!.GetValue(ok.Value)!);
            Assert.Equal("Messages marked as read.", ok.Value.GetType().GetProperty("Message")!.GetValue(ok.Value));
            Assert.False(fromClient.IsRead);
            Assert.True(fromConsultant.IsRead);
            Assert.True(fromAdmin.IsRead);
            Assert.False(otherCase.IsRead);

            // Idempotent: nothing left to mark.
            var again = Assert.IsType<OkObjectResult>(await CreateController(context, ClientId, "Client").MarkThreadRead(CaseId));
            Assert.Equal(0, (int)again.Value!.GetType().GetProperty("Updated")!.GetValue(again.Value)!);
        }

        [Fact]
        public async Task MarkRead_AssignedConsultant_MarksClientMessages()
        {
            using var context = await CreateContextAsync(nameof(MarkRead_AssignedConsultant_MarksClientMessages));
            var fromClient = AddMessage(context, CaseId, ClientId, "From client", DateTime.UtcNow.AddMinutes(-1));
            var fromConsultant = AddMessage(context, CaseId, ConsultantId, "From consultant", DateTime.UtcNow);
            await context.SaveChangesAsync();

            Assert.IsType<OkObjectResult>(await CreateController(context, ConsultantId, "Consultant").MarkThreadRead(CaseId));

            Assert.True(fromClient.IsRead);
            Assert.False(fromConsultant.IsRead);
        }

        // ── Ordering and paging ────────────────────────────────────────────

        [Fact]
        public async Task GetMessages_ReturnsOldestFirst()
        {
            using var context = await CreateContextAsync(nameof(GetMessages_ReturnsOldestFirst));
            var now = DateTime.UtcNow;
            // Inserted out of order on purpose.
            AddMessage(context, CaseId, ClientId, "third", now);
            AddMessage(context, CaseId, ConsultantId, "first", now.AddHours(-2));
            AddMessage(context, CaseId, AdminId, "second", now.AddHours(-1));
            await context.SaveChangesAsync();

            var thread = Page(await CreateController(context, ClientId, "Client").GetMessages(CaseId));

            Assert.Equal(new[] { "first", "second", "third" }, thread.Select(m => m.Body));
            Assert.Equal(new[] { "Consultant User", "System Admin", "John Doe" }, thread.Select(m => m.SenderName));
            Assert.Equal(new[] { "Consultant", "Admin", "Client" }, thread.Select(m => m.SenderRole));
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-5, 1)]
        [InlineData(500, 100)]
        public async Task GetMessages_ClampsPageSize(int requested, int expected)
        {
            using var context = await CreateContextAsync(nameof(GetMessages_ClampsPageSize) + requested);
            var start = DateTime.UtcNow.AddDays(-1);
            for (var i = 0; i < 120; i++)
                AddMessage(context, CaseId, ClientId, $"m{i}", start.AddMinutes(i));
            await context.SaveChangesAsync();

            var result = await CreateController(context, ConsultantId, "Consultant").GetMessages(CaseId, page: 1, pageSize: requested);

            var page = Assert.IsType<PagedResponse<MessageDto>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.Equal(expected, page.Data.Count());
            Assert.Equal(120, page.TotalRecords);
            Assert.Equal((int)Math.Ceiling(120 / (double)expected), page.TotalPages);
            Assert.Equal("m0", page.Data.First().Body);
        }

        [Fact]
        public async Task GetMessages_LastPage_HoldsNewestMessages()
        {
            // The web fetches page 1, then the last page when there is more than one.
            using var context = await CreateContextAsync(nameof(GetMessages_LastPage_HoldsNewestMessages));
            var start = DateTime.UtcNow.AddDays(-1);
            for (var i = 0; i < 55; i++)
                AddMessage(context, CaseId, ClientId, $"m{i}", start.AddMinutes(i));
            await context.SaveChangesAsync();

            var result = await CreateController(context, ClientId, "Client").GetMessages(CaseId, page: 2, pageSize: 50);

            var page = Assert.IsType<PagedResponse<MessageDto>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.Equal(2, page.TotalPages);
            Assert.Equal(2, page.CurrentPage);
            Assert.Equal(new[] { "m50", "m51", "m52", "m53", "m54" }, page.Data.Select(m => m.Body));
        }
    }
}
