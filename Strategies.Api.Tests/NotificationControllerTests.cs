using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// Integration tests for /api/notifications over HTTP, run against the real pipeline in a TestServer
    /// with an in-memory database (same pattern as AuthControllerTests): who sees which messages, the
    /// unread count, paging, and who may mark a message read.
    /// </summary>
    public class NotificationControllerTests : IDisposable
    {
        private readonly ApiFactory _factory;
        private readonly HttpClient _client;

        // Two cases: Client A with Consultant A, Client B with Consultant B.
        private readonly Guid _admin = Guid.NewGuid();
        private readonly Guid _clientA = Guid.NewGuid();
        private readonly Guid _clientB = Guid.NewGuid();
        private readonly Guid _consultantA = Guid.NewGuid();
        private readonly Guid _consultantB = Guid.NewGuid();
        private readonly Guid _caseA = Guid.NewGuid();
        private readonly Guid _caseB = Guid.NewGuid();

        public NotificationControllerTests()
        {
            _factory = new ApiFactory();
            _client = _factory.CreateClient();

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Cases.Add(NewCase(_caseA, _clientA, _consultantA));
            db.Cases.Add(NewCase(_caseB, _clientB, _consultantB));
            db.SaveChanges();
        }

        public void Dispose()
        {
            _client.Dispose();
            _factory.Dispose();
        }

        private sealed class ApiFactory : WebApplicationFactory<Program>
        {
            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment("Testing");
                foreach (var (key, value) in StartupConfigurationTests.RequiredTestSettings)
                    builder.UseSetting(key, value);

                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
                    services.RemoveAll(typeof(IDbContextOptionsConfiguration<ApplicationDbContext>));
                    var dbName = Guid.NewGuid().ToString();
                    services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));
                });
            }
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static Case NewCase(Guid id, Guid clientId, Guid consultantId) => new()
        {
            Id = id,
            ClientId = clientId,
            ConsultantId = consultantId,
            VisaCategoryId = Guid.NewGuid(),
            Stage = CaseStage.DocumentGathering,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        private string TokenFor(Guid userId, UserRole role)
        {
            var user = new User { Id = userId, FullName = "Test User", Email = "test@example.com", Role = role };
            return _factory.Services.GetRequiredService<ITokenService>().GenerateAccessToken(user).Token;
        }

        private async Task<Guid> AddMessageAsync(Guid caseId, Guid senderId, string body, bool isRead = false, int minutesAgo = 0)
        {
            var message = new Message
            {
                Id = Guid.NewGuid(),
                CaseId = caseId,
                SenderId = senderId,
                Body = body,
                IsRead = isRead,
                SentAt = DateTime.UtcNow.AddMinutes(-minutesAgo)
            };
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Messages.Add(message);
            await db.SaveChangesAsync();
            return message.Id;
        }

        private async Task<bool> IsReadAsync(Guid messageId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            return (await db.Messages.AsNoTracking().SingleAsync(m => m.Id == messageId)).IsRead;
        }

        private async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? bearer)
        {
            var request = new HttpRequestMessage(method, path);
            if (bearer != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            return await _client.SendAsync(request);
        }

        private async Task<JsonElement> GetListAsync(string bearer, string query = "")
        {
            var response = await Send(HttpMethod.Get, "/api/notifications" + query, bearer);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        }

        private static string[] Bodies(JsonElement list) =>
            list.GetProperty("data").EnumerateArray().Select(m => m.GetProperty("body").GetString()!).ToArray();

        private Task<HttpResponseMessage> MarkRead(Guid messageId, string bearer) =>
            Send(HttpMethod.Patch, $"/api/notifications/{messageId}/read", bearer);

        // ── GET /api/notifications ─────────────────────────────────────────

        [Fact]
        public async Task List_WithoutAToken_Returns401()
        {
            var response = await Send(HttpMethod.Get, "/api/notifications", bearer: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Client_SeesOnlyMessagesOnTheirOwnCase_NotTheirOwnMessages()
        {
            await AddMessageAsync(_caseA, _consultantA, "To client A");
            await AddMessageAsync(_caseA, _clientA, "From client A");
            await AddMessageAsync(_caseB, _consultantB, "To client B");

            var list = await GetListAsync(TokenFor(_clientA, UserRole.Client));

            Assert.Equal(new[] { "To client A" }, Bodies(list));
            Assert.Equal(1, list.GetProperty("totalRecords").GetInt32());
        }

        [Fact]
        public async Task Consultant_SeesOnlyMessagesOnAssignedCases_NotTheirOwnMessages()
        {
            await AddMessageAsync(_caseA, _clientA, "From client A");
            await AddMessageAsync(_caseA, _consultantA, "From consultant A");
            await AddMessageAsync(_caseB, _clientB, "From client B");

            var list = await GetListAsync(TokenFor(_consultantA, UserRole.Consultant));

            Assert.Equal(new[] { "From client A" }, Bodies(list));
        }

        [Fact]
        public async Task Admin_SeesMessagesOnEveryCase_ExceptTheirOwn()
        {
            await AddMessageAsync(_caseA, _clientA, "From client A", minutesAgo: 2);
            await AddMessageAsync(_caseB, _clientB, "From client B", minutesAgo: 1);
            await AddMessageAsync(_caseB, _admin, "From the admin");

            var list = await GetListAsync(TokenFor(_admin, UserRole.Admin));

            Assert.Equal(new[] { "From client B", "From client A" }, Bodies(list));
        }

        [Fact]
        public async Task List_IsNewestFirst_WithTheUnreadCount_AndOnlyTheListedFields()
        {
            await AddMessageAsync(_caseA, _consultantA, "Oldest", isRead: true, minutesAgo: 30);
            await AddMessageAsync(_caseA, _consultantA, "Newest", minutesAgo: 1);
            await AddMessageAsync(_caseA, _consultantA, "Middle", minutesAgo: 10);

            var list = await GetListAsync(TokenFor(_clientA, UserRole.Client));

            Assert.Equal(new[] { "Newest", "Middle", "Oldest" }, Bodies(list));
            Assert.Equal(2, list.GetProperty("unreadCount").GetInt32());
            var first = list.GetProperty("data")[0];
            Assert.Equal(new[] { "body", "caseId", "id", "isRead", "sentAt" },
                first.EnumerateObject().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal));
        }

        [Fact]
        public async Task Paging_ReturnsTheRequestedPage_AndCountsAcrossAllPages()
        {
            for (var i = 1; i <= 5; i++)
                await AddMessageAsync(_caseA, _consultantA, $"Message {i}", minutesAgo: 10 - i);

            var page2 = await GetListAsync(TokenFor(_clientA, UserRole.Client), "?page=2&pageSize=2");

            Assert.Equal(new[] { "Message 3", "Message 2" }, Bodies(page2));
            Assert.Equal(5, page2.GetProperty("totalRecords").GetInt32());
            Assert.Equal(3, page2.GetProperty("totalPages").GetInt32());
            Assert.Equal(2, page2.GetProperty("currentPage").GetInt32());
            Assert.Equal(5, page2.GetProperty("unreadCount").GetInt32());
        }

        // ── PATCH /api/notifications/{id}/read ─────────────────────────────

        [Fact]
        public async Task MarkRead_WithoutAToken_Returns401()
        {
            var id = await AddMessageAsync(_caseA, _consultantA, "To client A");

            var response = await Send(HttpMethod.Patch, $"/api/notifications/{id}/read", bearer: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.False(await IsReadAsync(id));
        }

        [Fact]
        public async Task MarkRead_ByTheRecipient_Returns200_AndIsIdempotent()
        {
            var id = await AddMessageAsync(_caseA, _consultantA, "To client A");
            var token = TokenFor(_clientA, UserRole.Client);

            var first = await MarkRead(id, token);
            var second = await MarkRead(id, token);

            Assert.Equal(HttpStatusCode.OK, first.StatusCode);
            Assert.Equal(HttpStatusCode.OK, second.StatusCode);
            Assert.Contains("Already marked as read.", await second.Content.ReadAsStringAsync());
            Assert.True(await IsReadAsync(id));
        }

        [Fact]
        public async Task MarkRead_OwnMessage_Returns403_AndStaysUnread()
        {
            var id = await AddMessageAsync(_caseA, _clientA, "From client A");

            var response = await MarkRead(id, TokenFor(_clientA, UserRole.Client));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.False(await IsReadAsync(id));
        }

        [Theory]
        [InlineData(UserRole.Client)]
        [InlineData(UserRole.Consultant)]
        public async Task MarkRead_OnSomeoneElsesCase_Returns403_AndStaysUnread(UserRole role)
        {
            var id = await AddMessageAsync(_caseA, _consultantA, "To client A");
            var outsider = role == UserRole.Client ? _clientB : _consultantB;

            var response = await MarkRead(id, TokenFor(outsider, role));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.False(await IsReadAsync(id));
        }

        [Fact]
        public async Task MarkRead_ByAnAdmin_OnAnyCase_Returns200()
        {
            var id = await AddMessageAsync(_caseB, _clientB, "From client B");

            var response = await MarkRead(id, TokenFor(_admin, UserRole.Admin));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(await IsReadAsync(id));
        }

        [Fact]
        public async Task MarkRead_UnknownMessage_Returns404()
        {
            var response = await MarkRead(Guid.NewGuid(), TokenFor(_clientA, UserRole.Client));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
