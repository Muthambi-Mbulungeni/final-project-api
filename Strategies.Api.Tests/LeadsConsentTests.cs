using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Strategies.Api.Controllers;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.DTOs;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    public class LeadsConsentTests
    {
        private sealed class DummyAgentService : IAgentService
        {
            public Task<AgentResponse> ProcessLeadAsync(string leadData)
            {
                return Task.FromResult(new AgentResponse
                {
                    Status = "Success",
                    Message = "Processed",
                    ExtractedData = "{\"status\":\"Qualified\",\"reason\":\"Valid application\"}"
                });
            }
        }

        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void CreateLeadRequest_WhenConsentIsFalse_FailsDtoValidation()
        {
            var dto = new CreateLeadRequest
            {
                FullName = "Jane Doe",
                Email = "jane@example.com",
                ImmigrationStatus = ImmigrationStatus.Citizen,
                NeedsAssistance = true,
                DesiredStartDate = DateTime.UtcNow.AddDays(30),
                Consent = false
            };

            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

            Assert.False(isValid);
            Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("POPIA"));
        }

        [Fact]
        public void CreateLeadRequest_WhenConsentIsTrue_PassesDtoValidation()
        {
            var dto = new CreateLeadRequest
            {
                FullName = "Jane Doe",
                Email = "jane@example.com",
                ImmigrationStatus = ImmigrationStatus.Citizen,
                NeedsAssistance = true,
                DesiredStartDate = DateTime.UtcNow.AddDays(30),
                Consent = true
            };

            var context = new ValidationContext(dto);
            var results = new List<ValidationResult>();
            var isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

            Assert.True(isValid);
            Assert.Empty(results);
        }

        [Fact]
        public async Task CreateLead_WithoutConsent_ReturnsBadRequestAndDoesNotPersist()
        {
            using var ctx = CreateContext(nameof(CreateLead_WithoutConsent_ReturnsBadRequestAndDoesNotPersist));
            var controller = new LeadsController(ctx);
            var agent = new DummyAgentService();

            var dto = new CreateLeadRequest
            {
                FullName = "Refused Consent Lead",
                Email = "refused@example.com",
                ImmigrationStatus = ImmigrationStatus.Foreigner,
                NeedsAssistance = false,
                DesiredStartDate = DateTime.UtcNow.AddDays(15),
                Consent = false
            };

            var result = await controller.CreateLead(dto, agent);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            var response = Assert.IsType<ApiErrorResponse>(badRequest.Value);
            Assert.Contains(response.Errors, err => err.Contains("POPIA"));
            Assert.Empty(await ctx.Leads.ToListAsync());
        }

        [Fact]
        public async Task CreateLead_WithConsent_SavesLeadWithConsentGivenAt()
        {
            using var ctx = CreateContext(nameof(CreateLead_WithConsent_SavesLeadWithConsentGivenAt));
            var controller = new LeadsController(ctx);
            var agent = new DummyAgentService();

            var before = DateTime.UtcNow.AddSeconds(-1);
            var dto = new CreateLeadRequest
            {
                FullName = "Consenting Lead",
                Email = "consent@example.com",
                ImmigrationStatus = ImmigrationStatus.TemporaryResident,
                NeedsAssistance = true,
                DesiredStartDate = DateTime.UtcNow.AddDays(20),
                Consent = true
            };

            var result = await controller.CreateLead(dto, agent);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var savedLead = await ctx.Leads.SingleOrDefaultAsync(l => l.Email == "consent@example.com");
            Assert.NotNull(savedLead);
            Assert.NotNull(savedLead.ConsentGivenAt);
            Assert.True(savedLead.ConsentGivenAt >= before && savedLead.ConsentGivenAt <= DateTime.UtcNow.AddSeconds(1));
        }
    }
}
