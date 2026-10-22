using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Strategies.Api.Configuration;
using Strategies.Api.DTOs;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// Integration tests for the POST /api/leads rate limit and the forwarded-headers setup, run against
    /// the real pipeline in a TestServer. Requests send an empty body, so the API answers 400 without
    /// touching the database or Gemini; the rate limiter runs before that, so each request still counts.
    /// </summary>
    public class LeadsRateLimitTests
    {
        // TestServer has no connection address. This header stands in for it (the Azure front end's
        // address, in production terms); a test-only startup filter copies it onto the connection.
        private const string RemoteIpHeader = "X-Test-Remote-Ip";

        private sealed class ApiFactory : WebApplicationFactory<Program>
        {
            private readonly Dictionary<string, string?> _settings;
            private readonly string _environment;

            public ApiFactory(Dictionary<string, string?>? settings = null, string environment = "Testing")
            {
                _settings = settings ?? new Dictionary<string, string?>();
                _environment = environment;
            }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder.UseEnvironment(_environment);
                foreach (var (key, value) in _settings)
                    builder.UseSetting(key, value);

                // Inserted first so it runs before every other startup filter, including the host's own
                // forwarded-headers filter, just as a real connection address exists before any middleware.
                builder.ConfigureTestServices(services =>
                    services.Insert(0, ServiceDescriptor.Singleton<IStartupFilter, TestRemoteIpStartupFilter>()));
            }
        }

        private sealed class TestRemoteIpStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
            {
                app.Use(async (context, nextMiddleware) =>
                {
                    if (IPAddress.TryParse(context.Request.Headers[RemoteIpHeader], out var address))
                        context.Connection.RemoteIpAddress = address;
                    await nextMiddleware();
                });
                next(app);
            };
        }

        private static async Task<HttpResponseMessage> PostLead(HttpClient client, string? remoteIp, string? forwardedFor = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/leads")
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };
            if (remoteIp != null)
                request.Headers.Add(RemoteIpHeader, remoteIp);
            if (forwardedFor != null)
                request.Headers.Add("X-Forwarded-For", forwardedFor);
            return await client.SendAsync(request);
        }

        private static async Task AssertAllowed(HttpClient client, int count, string? remoteIp, Func<int, string?>? forwardedFor = null)
        {
            for (var i = 0; i < count; i++)
            {
                var response = await PostLead(client, remoteIp, forwardedFor?.Invoke(i));
                Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }

        private static Dictionary<string, string?> ForwardingOn() => new() { ["ForwardedHeaders:Enabled"] = "true" };

        [Fact]
        public async Task SixthRequestInTheWindow_Gets429WithApiErrorResponseAndRetryAfter()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            for (var i = 0; i < 5; i++)
            {
                var allowed = await PostLead(client, "198.51.100.10");
                Assert.Equal(HttpStatusCode.BadRequest, allowed.StatusCode);
            }

            var rejected = await PostLead(client, "198.51.100.10");

            Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
            var body = await rejected.Content.ReadFromJsonAsync<ApiErrorResponse>();
            Assert.Equal("Too many requests. Please try again later.", body!.Message);
            Assert.Empty(body.Errors);
            var retryAfter = Assert.Single(rejected.Headers.GetValues("Retry-After"));
            var seconds = int.Parse(retryAfter);
            Assert.InRange(seconds, 1, 600);
        }

        [Fact]
        public async Task Limits_ComeFromConfiguration()
        {
            using var factory = new ApiFactory(new()
            {
                ["RateLimiting:Leads:PermitLimit"] = "2",
                ["RateLimiting:Leads:WindowSeconds"] = "30"
            });
            using var client = factory.CreateClient();

            await AssertAllowed(client, 2, "198.51.100.11");
            var rejected = await PostLead(client, "198.51.100.11");

            Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
            Assert.InRange(int.Parse(Assert.Single(rejected.Headers.GetValues("Retry-After"))), 1, 30);
        }

        [Fact]
        public async Task DifferentClientIps_HaveSeparateLimits()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            await AssertAllowed(client, 5, "198.51.100.20");
            Assert.Equal(HttpStatusCode.TooManyRequests, (await PostLead(client, "198.51.100.20")).StatusCode);

            await AssertAllowed(client, 5, "198.51.100.21");
            Assert.Equal(HttpStatusCode.TooManyRequests, (await PostLead(client, "198.51.100.21")).StatusCode);
        }

        [Fact]
        public async Task NoClientIp_SharesOneUnknownPartition()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            await AssertAllowed(client, 5, remoteIp: null);

            Assert.Equal(HttpStatusCode.TooManyRequests, (await PostLead(client, remoteIp: null)).StatusCode);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await PostLead(client, "198.51.100.30")).StatusCode);
        }

        [Fact]
        public async Task ForwardedHeadersOff_RotatingXForwardedFor_DoesNotResetTheLimit()
        {
            using var factory = new ApiFactory();
            using var client = factory.CreateClient();

            await AssertAllowed(client, 5, "198.51.100.40", i => $"203.0.113.{i + 1}");
            var rejected = await PostLead(client, "198.51.100.40", "203.0.113.99");

            Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        }

        [Fact]
        public async Task ForwardedHeadersOn_OnlyTheRightmostEntryDecides_SpoofedLeftEntriesAreIgnored()
        {
            using var factory = new ApiFactory(ForwardingOn());
            using var client = factory.CreateClient();

            // Same front end, same real client (rightmost), a new made-up address on the left every time.
            await AssertAllowed(client, 5, "10.0.0.1", i => $"203.0.113.{i + 1}, 198.51.100.50");
            var rejected = await PostLead(client, "10.0.0.1", "203.0.113.99, 198.51.100.50");
            Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);

            // A different real client behind the same front end has its own limit.
            Assert.NotEqual(HttpStatusCode.TooManyRequests, (await PostLead(client, "10.0.0.1", "198.51.100.51")).StatusCode);
        }

        [Fact]
        public async Task ForwardedHeadersOn_WithAzureHostSwitch_ProcessesTheHeaderOnlyOnce()
        {
            // Azure App Service on Linux sets ASPNETCORE_FORWARDEDHEADERS_ENABLED=true. Processing the header
            // twice would let the second-to-last (made-up) entry decide, so rotating it would bypass the limit.
            var settings = ForwardingOn();
            settings["FORWARDEDHEADERS_ENABLED"] = "true";
            using var factory = new ApiFactory(settings);
            using var client = factory.CreateClient();

            await AssertAllowed(client, 5, "10.0.0.2", i => $"203.0.113.{i + 1}, 198.51.100.60");
            var rejected = await PostLead(client, "10.0.0.2", "203.0.113.99, 198.51.100.60");

            Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        }

        [Fact]
        public async Task OtherEndpoints_AreNotRateLimited()
        {
            using var factory = new ApiFactory(new() { ["RateLimiting:Leads:PermitLimit"] = "1" });
            using var client = factory.CreateClient();

            await AssertAllowed(client, 1, "198.51.100.70");
            Assert.Equal(HttpStatusCode.TooManyRequests, (await PostLead(client, "198.51.100.70")).StatusCode);

            for (var i = 0; i < 10; i++)
            {
                var list = new HttpRequestMessage(HttpMethod.Get, "/api/leads");
                list.Headers.Add(RemoteIpHeader, "198.51.100.70");
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(list)).StatusCode);

                var review = new HttpRequestMessage(HttpMethod.Patch, $"/api/leads/{Guid.NewGuid()}/status")
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
                review.Headers.Add(RemoteIpHeader, "198.51.100.70");
                Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(review)).StatusCode);
            }
        }

        [Fact]
        public void ForwardedHeaders_AreOffByDefault_AndOnInProduction()
        {
            using var testing = new ApiFactory();
            using var production = new ApiFactory(environment: "Production");

            Assert.False(testing.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value.Enabled);
            var prod = production.Services.GetRequiredService<IOptions<ForwardedHeadersSettings>>().Value;
            Assert.True(prod.Enabled);
            Assert.Equal(1, prod.ForwardLimit);
            Assert.Empty(prod.KnownProxies);
        }

        [Theory]
        [InlineData("ForwardedHeaders:KnownProxies:0", "not-an-ip")]
        [InlineData("ForwardedHeaders:KnownNetworks:0", "10.0.0.0/99")]
        [InlineData("ForwardedHeaders:ForwardLimit", "0")]
        [InlineData("RateLimiting:Leads:PermitLimit", "0")]
        [InlineData("RateLimiting:Leads:WindowSeconds", "-5")]
        public void InvalidSettings_StopTheApiFromStarting(string key, string value)
        {
            using var factory = new ApiFactory(new() { [key] = value });

            Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        }

        [Fact]
        public void ConfiguredProxies_ReplaceTheDefaults()
        {
            var settings = new ForwardedHeadersSettings
            {
                Enabled = true,
                KnownProxies = { "10.1.2.3" },
                KnownNetworks = { "172.16.0.0/12" }
            };
            var options = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions();

            settings.Apply(options);

            Assert.Equal(new[] { IPAddress.Parse("10.1.2.3") }, options.KnownProxies);
            Assert.Equal(new[] { System.Net.IPNetwork.Parse("172.16.0.0/12") }, options.KnownIPNetworks);
            Assert.Equal(1, options.ForwardLimit);
        }
    }

    public class ClientPartitionKeyTests
    {
        [Fact]
        public void NullAddress_IsTheSharedUnknownPartition() =>
            Assert.Equal("unknown", ClientPartitionKey.From(null));

        [Fact]
        public void Ipv4MappedIpv6_CountsAsTheIpv4Address() =>
            Assert.Equal("198.51.100.1", ClientPartitionKey.From(IPAddress.Parse("::ffff:198.51.100.1")));

        [Fact]
        public void Ipv6AddressesInOneSlash64_ShareAPartition()
        {
            var a = ClientPartitionKey.From(IPAddress.Parse("2001:db8:1:2:aaaa:bbbb:cccc:1"));
            var b = ClientPartitionKey.From(IPAddress.Parse("2001:db8:1:2:1234:5678:9abc:def0"));
            var other = ClientPartitionKey.From(IPAddress.Parse("2001:db8:1:3::1"));

            Assert.Equal("2001:db8:1:2::/64", a);
            Assert.Equal(a, b);
            Assert.NotEqual(a, other);
        }
    }
}
