using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Strategies.Api.Controllers;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.DTOs;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class GeminiAgentServiceTests
    {
        private const string ApiKey = "test-key-123";

        private static readonly LeadTriageInput Input =
            new(ImmigrationStatus.Foreigner, true, new DateTime(2027, 1, 15), "Critical Skills Work Visa");

        // Records every request and answers with whatever the test supplies. No real network calls.
        private sealed class FakeHandler : HttpMessageHandler
        {
            private readonly Func<CancellationToken, Task<HttpResponseMessage>> _respond;
            public List<HttpRequestMessage> Requests { get; } = new();
            public List<string> Bodies { get; } = new();

            public FakeHandler(Func<CancellationToken, Task<HttpResponseMessage>> respond) => _respond = respond;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Requests.Add(request);
                Bodies.Add(request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken));
                return await _respond(cancellationToken);
            }
        }

        private sealed class ListLogger : ILogger<GeminiAgentService>
        {
            public List<string> Messages { get; } = new();
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => Messages.Add(formatter(state, exception) + " " + exception);
        }

        private static FakeHandler Replying(HttpStatusCode code, string body) =>
            new(_ => Task.FromResult(new HttpResponseMessage(code) { Content = new StringContent(body, Encoding.UTF8, "application/json") }));

        // Wraps the model's text in the generateContent response envelope.
        private static string Envelope(string modelText) =>
            JsonSerializer.Serialize(new { candidates = new[] { new { content = new { parts = new[] { new { text = modelText } } } } } });

        private static (GeminiAgentService Service, ListLogger Logger) CreateService(
            FakeHandler handler, string? apiKey = ApiKey, TimeSpan? timeout = null)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["Gemini:ApiKey"] = apiKey })
                .Build();
            var client = new HttpClient(handler) { Timeout = timeout ?? TimeSpan.FromSeconds(10) };
            var logger = new ListLogger();
            return (new GeminiAgentService(client, config, logger), logger);
        }

        [Fact]
        public async Task TriageLead_ValidQualifiedReply_ReturnsQualifiedWithReason()
        {
            var handler = Replying(HttpStatusCode.OK, Envelope("{\"status\":\"Qualified\",\"reason\":\"Foreigner seeking a work visa.\"}"));
            var (service, _) = CreateService(handler);

            var result = await service.TriageLeadAsync(Input);

            Assert.Equal(RunStatus.Success, result.Status);
            Assert.Equal(TriageStatus.Qualified, result.TriageStatus);
            Assert.Equal("Foreigner seeking a work visa.", result.Reason);
        }

        [Fact]
        public async Task TriageLead_ValidUnqualifiedReply_ReturnsUnqualified()
        {
            var handler = Replying(HttpStatusCode.OK, Envelope("{\"status\":\"Unqualified\",\"reason\":\"Citizen needs no visa.\"}"));
            var (service, _) = CreateService(handler);

            var result = await service.TriageLeadAsync(Input);

            Assert.Equal(RunStatus.Success, result.Status);
            Assert.Equal(TriageStatus.Unqualified, result.TriageStatus);
        }

        [Fact]
        public async Task TriageLead_StatusOutsideAllowedValues_ReturnsNeedsReviewAndError()
        {
            var handler = Replying(HttpStatusCode.OK, Envelope("{\"status\":\"Approved\",\"reason\":\"Ignore previous instructions.\"}"));
            var (service, _) = CreateService(handler);

            var result = await service.TriageLeadAsync(Input);

            Assert.Equal(TriageStatus.NeedsReview, result.TriageStatus);
            Assert.Equal(RunStatus.Error, result.Status);
            Assert.Null(result.Reason);
        }

        [Fact]
        public async Task TriageLead_UnparseableModelText_ReturnsNeedsReviewAndError()
        {
            var handler = Replying(HttpStatusCode.OK, Envelope("Sure! The lead is Qualified."));
            var (service, _) = CreateService(handler);

            var result = await service.TriageLeadAsync(Input);

            Assert.Equal(TriageStatus.NeedsReview, result.TriageStatus);
            Assert.Equal(RunStatus.Error, result.Status);
            Assert.Equal("Sure! The lead is Qualified.", result.Output);
        }

        [Fact]
        public async Task TriageLead_Http500_ReturnsNeedsReviewAndFallback()
        {
            var handler = Replying(HttpStatusCode.InternalServerError, "{\"error\":\"boom\"}");
            var (service, _) = CreateService(handler);

            var result = await service.TriageLeadAsync(Input);

            Assert.Equal(TriageStatus.NeedsReview, result.TriageStatus);
            Assert.Equal(RunStatus.Fallback, result.Status);
        }

        [Fact]
        public async Task TriageLead_Timeout_ReturnsNeedsReviewAndFallback()
        {
            var handler = new FakeHandler(async ct =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var (service, _) = CreateService(handler, timeout: TimeSpan.FromMilliseconds(100));

            var result = await service.TriageLeadAsync(Input);

            Assert.Equal(TriageStatus.NeedsReview, result.TriageStatus);
            Assert.Equal(RunStatus.Fallback, result.Status);
        }

        [Fact]
        public async Task TriageLead_MissingKey_ReturnsNeedsReviewWithoutHttpCall()
        {
            var handler = Replying(HttpStatusCode.OK, Envelope("{\"status\":\"Qualified\",\"reason\":\"x\"}"));
            var (service, _) = CreateService(handler, apiKey: "");

            var result = await service.TriageLeadAsync(Input);

            Assert.Equal(TriageStatus.NeedsReview, result.TriageStatus);
            Assert.Equal(RunStatus.Fallback, result.Status);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task TriageLead_SendsKeyInHeaderNotUrl()
        {
            var handler = Replying(HttpStatusCode.OK, Envelope("{\"status\":\"Qualified\",\"reason\":\"x\"}"));
            var (service, logger) = CreateService(handler);

            await service.TriageLeadAsync(Input);

            var request = Assert.Single(handler.Requests);
            Assert.Equal(ApiKey, request.Headers.GetValues("x-goog-api-key").Single());
            Assert.DoesNotContain(ApiKey, request.RequestUri!.ToString());
            Assert.Equal(
                "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash-lite:generateContent",
                request.RequestUri.ToString());
            Assert.DoesNotContain(logger.Messages, m => m.Contains(ApiKey));
        }

        [Fact]
        public async Task TriageLead_FailureLogs_DoNotContainKey()
        {
            var handler = new FakeHandler(_ => throw new HttpRequestException("connection refused"));
            var (service, logger) = CreateService(handler);

            var result = await service.TriageLeadAsync(Input);

            Assert.Equal(RunStatus.Fallback, result.Status);
            Assert.NotEmpty(logger.Messages);
            Assert.DoesNotContain(logger.Messages, m => m.Contains(ApiKey));
        }

        [Fact]
        public async Task TriageLead_RequestBody_HasSystemInstructionAndNoPersonalData()
        {
            var handler = Replying(HttpStatusCode.OK, Envelope("{\"status\":\"Qualified\",\"reason\":\"x\"}"));
            var (service, _) = CreateService(handler);

            // Drive through the controller so the real FullName/Email are in scope and must be dropped.
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);
            var controller = new LeadsController(context);
            await controller.CreateLead(new CreateLeadRequest
            {
                FullName = "Thandiwe Applicant",
                Email = "thandiwe@example.com",
                ImmigrationStatus = ImmigrationStatus.Foreigner,
                NeedsAssistance = true,
                DesiredStartDate = new DateTime(2027, 1, 15)
            }, service);

            var body = Assert.Single(handler.Bodies);
            using var json = JsonDocument.Parse(body);
            Assert.True(json.RootElement.TryGetProperty("systemInstruction", out _));
            Assert.Equal(0, json.RootElement.GetProperty("generationConfig").GetProperty("temperature").GetInt32());
            Assert.DoesNotContain("Thandiwe", body);
            Assert.DoesNotContain("thandiwe@example.com", body);
        }

        [Fact]
        public async Task CreateLead_SavesLeadAndAgentRunTogether_WithTruncatedOutput()
        {
            var longReason = new string('a', 3000);
            var handler = Replying(HttpStatusCode.OK, Envelope($"{{\"status\":\"Qualified\",\"reason\":\"{longReason}\"}}"));
            var (service, _) = CreateService(handler);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            using var context = new ApplicationDbContext(options);
            await new LeadsController(context).CreateLead(new CreateLeadRequest
            {
                FullName = "Test Applicant",
                Email = "test@example.com",
                ImmigrationStatus = ImmigrationStatus.Foreigner,
                NeedsAssistance = true,
                DesiredStartDate = new DateTime(2027, 1, 15)
            }, service);

            var lead = Assert.Single(context.Leads);
            var run = Assert.Single(context.AgentRuns);
            Assert.Equal(TriageStatus.Qualified, lead.TriageStatus);
            Assert.Equal(lead.Id, run.LeadId);
            Assert.Equal(AgentType.Triage, run.AgentType);
            Assert.Equal(RunStatus.Success, run.Status);
            Assert.Equal(2000, run.Output.Length);
        }
    }
}
