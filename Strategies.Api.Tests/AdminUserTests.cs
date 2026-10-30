using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Strategies.Api.Controllers;
using Strategies.Api.Data;
using Strategies.Api.Data.Enums;
using Strategies.Api.Data.Models;
using Strategies.Api.DTOs;
using Strategies.Api.DTOs.Auth;
using Strategies.Api.Services;
using Xunit;

namespace Strategies.Api.Tests
{
    /// <summary>
    /// Admin user management: PATCH /api/admin/users/{id} and the difference between
    /// suspending (IsActive = false) and deleting (DeletedAt set) an account.
    /// </summary>
    public class AdminUserTests
    {
        private static readonly Guid CallerId = Guid.NewGuid();

        private static ApplicationDbContext CreateContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new ApplicationDbContext(options);
        }

        private static AdminController CreateController(ApplicationDbContext ctx, Guid? callerId = null)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, (callerId ?? CallerId).ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            }, "Test");

            return new AdminController(ctx)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        private static async Task<User> SeedUser(
            ApplicationDbContext ctx,
            UserRole role,
            string email,
            string fullName = "Test User",
            bool isActive = true,
            DateTime? deletedAt = null,
            Guid? id = null)
        {
            var user = new User
            {
                Id = id ?? Guid.NewGuid(),
                FullName = fullName,
                Email = email,
                PasswordHash = "hash",
                Role = role,
                IsActive = isActive,
                EmailConfirmed = true,
                DeletedAt = deletedAt
            };
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            return user;
        }

        private static async Task SeedCase(ApplicationDbContext ctx, Guid consultantId, CaseStage stage, bool archived = false)
        {
            ctx.Cases.Add(new Case
            {
                Id = Guid.NewGuid(),
                ClientId = Guid.NewGuid(),
                ConsultantId = consultantId,
                VisaCategoryId = Guid.NewGuid(),
                Stage = stage,
                IsArchived = archived
            });
            await ctx.SaveChangesAsync();
        }

        private static ApiErrorResponse ErrorBody(IActionResult result) =>
            Assert.IsType<ApiErrorResponse>(Assert.IsAssignableFrom<ObjectResult>(result).Value);

        // ---------- PATCH: success ----------

        [Fact]
        public async Task Update_AllFields_Returns200WithUserDtoAndPersists()
        {
            using var ctx = CreateContext(nameof(Update_AllFields_Returns200WithUserDtoAndPersists));
            var user = await SeedUser(ctx, UserRole.Client, "old@example.com", "Old Name");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest
            {
                FullName = "  New Name ",
                Email = " New@Example.com ",
                Role = "consultant"
            });

            var dto = Assert.IsType<UserDto>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.Equal(user.Id, dto.Id);
            Assert.Equal("New Name", dto.FullName);
            Assert.Equal("new@example.com", dto.Email);
            Assert.Equal("Consultant", dto.Role);
            Assert.True(dto.IsActive);

            var saved = await ctx.Users.FindAsync(user.Id);
            Assert.Equal("New Name", saved!.FullName);
            Assert.Equal("new@example.com", saved.Email);
            Assert.Equal(UserRole.Consultant, saved.Role);
        }

        [Fact]
        public async Task Update_OnlyFieldsSent_AreChanged()
        {
            using var ctx = CreateContext(nameof(Update_OnlyFieldsSent_AreChanged));
            var user = await SeedUser(ctx, UserRole.Client, "keep@example.com", "Old Name");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest { FullName = "New Name" });

            Assert.IsType<OkObjectResult>(result);
            var saved = await ctx.Users.FindAsync(user.Id);
            Assert.Equal("New Name", saved!.FullName);
            Assert.Equal("keep@example.com", saved.Email);
            Assert.Equal(UserRole.Client, saved.Role);
        }

        [Fact]
        public async Task Update_Email_DoesNotChangeEmailConfirmed()
        {
            using var ctx = CreateContext(nameof(Update_Email_DoesNotChangeEmailConfirmed));
            var confirmed = await SeedUser(ctx, UserRole.Client, "a@example.com");
            var unconfirmed = await SeedUser(ctx, UserRole.Client, "b@example.com");
            unconfirmed.EmailConfirmed = false;
            await ctx.SaveChangesAsync();

            await CreateController(ctx).UpdateUser(confirmed.Id, new UpdateUserRequest { Email = "a2@example.com" });
            await CreateController(ctx).UpdateUser(unconfirmed.Id, new UpdateUserRequest { Email = "b2@example.com" });

            Assert.True((await ctx.Users.FindAsync(confirmed.Id))!.EmailConfirmed);
            Assert.False((await ctx.Users.FindAsync(unconfirmed.Id))!.EmailConfirmed);
        }

        [Fact]
        public async Task Update_WritesOneAuditRowWithOldAndNewValues()
        {
            using var ctx = CreateContext(nameof(Update_WritesOneAuditRowWithOldAndNewValues));
            var user = await SeedUser(ctx, UserRole.Client, "old@example.com", "Old Name");

            await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest
            {
                FullName = "New Name",
                Email = "new@example.com",
                Role = "Consultant"
            });

            var audit = Assert.Single(await ctx.AuditLogs.ToListAsync());
            Assert.Equal(CallerId, audit.UserId);
            Assert.Equal("User", audit.EntityType);
            Assert.Equal(user.Id, audit.EntityId);
            Assert.StartsWith("Updated user: ", audit.Action);
            Assert.Contains("name \"Old Name\" to \"New Name\"", audit.Action);
            Assert.Contains("email old@example.com to new@example.com", audit.Action);
            Assert.Contains("role Client to Consultant", audit.Action);
        }

        [Fact]
        public async Task Update_AuditRowListsOnlyChangedFields()
        {
            using var ctx = CreateContext(nameof(Update_AuditRowListsOnlyChangedFields));
            var user = await SeedUser(ctx, UserRole.Client, "same@example.com", "Old Name");

            await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest
            {
                FullName = "New Name",
                Email = "SAME@example.com",
                Role = "Client"
            });

            var audit = Assert.Single(await ctx.AuditLogs.ToListAsync());
            Assert.Equal("Updated user: name \"Old Name\" to \"New Name\"", audit.Action);
        }

        [Fact]
        public async Task Update_NothingActuallyChanged_Returns200WithoutAuditRow()
        {
            using var ctx = CreateContext(nameof(Update_NothingActuallyChanged_Returns200WithoutAuditRow));
            var user = await SeedUser(ctx, UserRole.Client, "same@example.com", "Same Name");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest
            {
                FullName = "Same Name",
                Email = "same@example.com",
                Role = "Client"
            });

            Assert.IsType<OkObjectResult>(result);
            Assert.Empty(await ctx.AuditLogs.ToListAsync());
        }

        // ---------- PATCH: 400 ----------

        [Fact]
        public async Task Update_EmptyBody_Returns400()
        {
            using var ctx = CreateContext(nameof(Update_EmptyBody_Returns400));
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest());

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.NotEmpty(ErrorBody(result).Errors);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Update_BlankFullName_Returns400(string fullName)
        {
            using var ctx = CreateContext(nameof(Update_BlankFullName_Returns400) + fullName.Length);
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com", "Kept Name");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest { FullName = fullName });

            Assert.IsType<BadRequestObjectResult>(result);
            var body = ErrorBody(result);
            Assert.Equal("Validation failed.", body.Message);
            Assert.Contains("Full name is required.", body.Errors);
            Assert.Equal("Kept Name", (await ctx.Users.FindAsync(user.Id))!.FullName);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-an-email")]
        [InlineData("two words@example.com")]
        [InlineData("@example.com")]
        public async Task Update_InvalidEmail_Returns400(string email)
        {
            using var ctx = CreateContext(nameof(Update_InvalidEmail_Returns400) + email);
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest { Email = email });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("A valid email address is required.", ErrorBody(result).Errors);
        }

        [Theory]
        [InlineData("Superuser")]
        [InlineData("")]
        [InlineData("1")]
        [InlineData("Admin, Client")]
        public async Task Update_InvalidRole_Returns400(string role)
        {
            using var ctx = CreateContext(nameof(Update_InvalidRole_Returns400) + role);
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest { Role = role });

            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Role must be one of: Admin, Consultant, Client.", ErrorBody(result).Errors);
            Assert.Equal(UserRole.Client, (await ctx.Users.FindAsync(user.Id))!.Role);
        }

        [Fact]
        public async Task Update_SeveralInvalidFields_ReportsEachError()
        {
            using var ctx = CreateContext(nameof(Update_SeveralInvalidFields_ReportsEachError));
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest
            {
                FullName = " ",
                Email = "bad",
                Role = "Boss"
            });

            Assert.Equal(3, ErrorBody(result).Errors.Count);
        }

        // ---------- PATCH: 404 ----------

        [Fact]
        public async Task Update_UnknownUser_Returns404()
        {
            using var ctx = CreateContext(nameof(Update_UnknownUser_Returns404));

            var result = await CreateController(ctx).UpdateUser(Guid.NewGuid(), new UpdateUserRequest { FullName = "X" });

            Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found.", ErrorBody(result).Message);
        }

        [Fact]
        public async Task Update_DeletedUser_Returns404AndChangesNothing()
        {
            using var ctx = CreateContext(nameof(Update_DeletedUser_Returns404AndChangesNothing));
            var user = await SeedUser(ctx, UserRole.Client, "gone@example.com", "Gone", isActive: false, deletedAt: DateTime.UtcNow);

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest { FullName = "Back" });

            Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Gone", (await ctx.Users.FindAsync(user.Id))!.FullName);
        }

        // ---------- PATCH: 409 duplicate email ----------

        [Fact]
        public async Task Update_DuplicateEmail_IgnoringCase_Returns409()
        {
            using var ctx = CreateContext(nameof(Update_DuplicateEmail_IgnoringCase_Returns409));
            await SeedUser(ctx, UserRole.Client, "Taken@Example.com");
            var user = await SeedUser(ctx, UserRole.Client, "mine@example.com");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest { Email = "taken@EXAMPLE.com" });

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Another account already uses this email address.", ErrorBody(result).Message);
            Assert.Equal("duplicate_email", ErrorBody(result).Code);
            Assert.Equal("mine@example.com", (await ctx.Users.FindAsync(user.Id))!.Email);
        }

        [Fact]
        public async Task Update_EmailOfDeletedAccount_Returns409()
        {
            using var ctx = CreateContext(nameof(Update_EmailOfDeletedAccount_Returns409));
            await SeedUser(ctx, UserRole.Client, "gone@example.com", isActive: false, deletedAt: DateTime.UtcNow);
            var user = await SeedUser(ctx, UserRole.Client, "mine@example.com");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest { Email = "gone@example.com" });

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Update_OwnEmailInOtherCase_IsNotADuplicate()
        {
            using var ctx = CreateContext(nameof(Update_OwnEmailInOtherCase_IsNotADuplicate));
            var user = await SeedUser(ctx, UserRole.Client, "mine@example.com");

            var result = await CreateController(ctx).UpdateUser(user.Id, new UpdateUserRequest { Email = "MINE@example.com" });

            Assert.IsType<OkObjectResult>(result);
        }

        // ---------- PATCH: 409 admin demotion ----------

        [Fact]
        public async Task Update_DemoteYourself_Returns409()
        {
            using var ctx = CreateContext(nameof(Update_DemoteYourself_Returns409));
            var me = await SeedUser(ctx, UserRole.Admin, "me@example.com", id: CallerId);
            await SeedUser(ctx, UserRole.Admin, "other-admin@example.com");

            var result = await CreateController(ctx).UpdateUser(me.Id, new UpdateUserRequest { Role = "Client" });

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("You cannot change your own role. Ask another administrator.", ErrorBody(result).Message);
            Assert.Equal(UserRole.Admin, (await ctx.Users.FindAsync(me.Id))!.Role);
        }

        [Fact]
        public async Task Update_OwnNameWithOwnRoleUnchanged_IsAllowed()
        {
            using var ctx = CreateContext(nameof(Update_OwnNameWithOwnRoleUnchanged_IsAllowed));
            var me = await SeedUser(ctx, UserRole.Admin, "me@example.com", id: CallerId);

            var result = await CreateController(ctx).UpdateUser(me.Id, new UpdateUserRequest { FullName = "New Me", Role = "Admin" });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_DemoteLastActiveAdmin_Returns409()
        {
            // The caller is the built-in admin account, which is not a row in this database.
            using var ctx = CreateContext(nameof(Update_DemoteLastActiveAdmin_Returns409));
            var admin = await SeedUser(ctx, UserRole.Admin, "only-admin@example.com");
            await SeedUser(ctx, UserRole.Admin, "suspended-admin@example.com", isActive: false);
            await SeedUser(ctx, UserRole.Admin, "deleted-admin@example.com", isActive: false, deletedAt: DateTime.UtcNow);

            var result = await CreateController(ctx).UpdateUser(admin.Id, new UpdateUserRequest { Role = "Consultant" });

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("last active administrator", ErrorBody(result).Message);
            Assert.Null(ErrorBody(result).Code);
            Assert.Equal(UserRole.Admin, (await ctx.Users.FindAsync(admin.Id))!.Role);
        }

        [Fact]
        public async Task Update_DemoteAdmin_WhenAnotherActiveAdminExists_Returns200()
        {
            using var ctx = CreateContext(nameof(Update_DemoteAdmin_WhenAnotherActiveAdminExists_Returns200));
            var admin = await SeedUser(ctx, UserRole.Admin, "admin-a@example.com");
            await SeedUser(ctx, UserRole.Admin, "admin-b@example.com");

            var result = await CreateController(ctx).UpdateUser(admin.Id, new UpdateUserRequest { Role = "Consultant" });

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(UserRole.Consultant, (await ctx.Users.FindAsync(admin.Id))!.Role);
        }

        [Fact]
        public async Task Update_DemoteSuspendedAdmin_WhenNoOtherActiveAdmin_Returns200()
        {
            // A suspended admin is not one of the active admins, so demoting them leaves the count unchanged.
            using var ctx = CreateContext(nameof(Update_DemoteSuspendedAdmin_WhenNoOtherActiveAdmin_Returns200));
            var admin = await SeedUser(ctx, UserRole.Admin, "suspended@example.com", isActive: false);

            var result = await CreateController(ctx).UpdateUser(admin.Id, new UpdateUserRequest { Role = "Client" });

            Assert.IsType<OkObjectResult>(result);
        }

        // ---------- PATCH: 409 consultant with active cases ----------

        [Fact]
        public async Task Update_ConsultantWithActiveCases_Returns409WithCount()
        {
            using var ctx = CreateContext(nameof(Update_ConsultantWithActiveCases_Returns409WithCount));
            var consultant = await SeedUser(ctx, UserRole.Consultant, "c@example.com");
            await SeedCase(ctx, consultant.Id, CaseStage.DocumentGathering);
            await SeedCase(ctx, consultant.Id, CaseStage.UnderReview);
            await SeedCase(ctx, consultant.Id, CaseStage.FinalDecision);
            await SeedCase(ctx, consultant.Id, CaseStage.InitialIntake, archived: true);

            var result = await CreateController(ctx).UpdateUser(consultant.Id, new UpdateUserRequest { Role = "Client" });

            Assert.IsType<ConflictObjectResult>(result);
            var message = ErrorBody(result).Message;
            Assert.Contains("2 active assigned cases", message);
            Assert.Contains("Reassign", message);
            Assert.Equal(UserRole.Consultant, (await ctx.Users.FindAsync(consultant.Id))!.Role);
        }

        [Fact]
        public async Task Update_ConsultantWithOneActiveCase_UsesSingular()
        {
            using var ctx = CreateContext(nameof(Update_ConsultantWithOneActiveCase_UsesSingular));
            var consultant = await SeedUser(ctx, UserRole.Consultant, "c@example.com");
            await SeedCase(ctx, consultant.Id, CaseStage.SubmittedToAuthority);

            var result = await CreateController(ctx).UpdateUser(consultant.Id, new UpdateUserRequest { Role = "Admin" });

            Assert.Contains("1 active assigned case.", ErrorBody(result).Message);
        }

        [Fact]
        public async Task Update_ConsultantWithOnlyClosedCases_Returns200()
        {
            using var ctx = CreateContext(nameof(Update_ConsultantWithOnlyClosedCases_Returns200));
            var consultant = await SeedUser(ctx, UserRole.Consultant, "c@example.com");
            await SeedCase(ctx, consultant.Id, CaseStage.FinalDecision);
            await SeedCase(ctx, consultant.Id, CaseStage.UnderReview, archived: true);

            var result = await CreateController(ctx).UpdateUser(consultant.Id, new UpdateUserRequest { Role = "Client" });

            Assert.IsType<OkObjectResult>(result);
        }

        [Fact]
        public async Task Update_ConsultantWithActiveCases_CanStillChangeNameAndEmail()
        {
            using var ctx = CreateContext(nameof(Update_ConsultantWithActiveCases_CanStillChangeNameAndEmail));
            var consultant = await SeedUser(ctx, UserRole.Consultant, "c@example.com");
            await SeedCase(ctx, consultant.Id, CaseStage.UnderReview);

            var result = await CreateController(ctx).UpdateUser(consultant.Id, new UpdateUserRequest
            {
                FullName = "Renamed",
                Email = "c2@example.com",
                Role = "Consultant"
            });

            Assert.IsType<OkObjectResult>(result);
        }

        // ---------- Delete vs suspend ----------

        [Fact]
        public async Task Delete_SetsDeletedAtAndIsActiveFalse_AndWritesAudit()
        {
            using var ctx = CreateContext(nameof(Delete_SetsDeletedAtAndIsActiveFalse_AndWritesAudit));
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com");

            var result = await CreateController(ctx).DeleteUser(user.Id);

            Assert.IsType<OkObjectResult>(result);
            var saved = await ctx.Users.FindAsync(user.Id);
            Assert.NotNull(saved!.DeletedAt);
            Assert.False(saved.IsActive);
            var audit = Assert.Single(await ctx.AuditLogs.ToListAsync());
            Assert.Equal("Deleted user", audit.Action);
            Assert.Equal(user.Id, audit.EntityId);
        }

        [Fact]
        public async Task Delete_SuspendedUser_IsAllowed()
        {
            using var ctx = CreateContext(nameof(Delete_SuspendedUser_IsAllowed));
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com", isActive: false);

            var result = await CreateController(ctx).DeleteUser(user.Id);

            Assert.IsType<OkObjectResult>(result);
            Assert.NotNull((await ctx.Users.FindAsync(user.Id))!.DeletedAt);
        }

        [Fact]
        public async Task Delete_AlreadyDeletedOrUnknown_Returns404()
        {
            using var ctx = CreateContext(nameof(Delete_AlreadyDeletedOrUnknown_Returns404));
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com", isActive: false, deletedAt: DateTime.UtcNow);

            Assert.IsType<NotFoundObjectResult>(await CreateController(ctx).DeleteUser(user.Id));
            Assert.IsType<NotFoundObjectResult>(await CreateController(ctx).DeleteUser(Guid.NewGuid()));
        }

        [Fact]
        public async Task Reinstate_DeletedUser_Returns409AndStaysDeleted()
        {
            using var ctx = CreateContext(nameof(Reinstate_DeletedUser_Returns409AndStaysDeleted));
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com");
            await CreateController(ctx).DeleteUser(user.Id);

            var result = await CreateController(ctx).ReinstateUser(user.Id);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("Deleted accounts cannot be reinstated.", ErrorBody(result).Message);
            var saved = await ctx.Users.FindAsync(user.Id);
            Assert.False(saved!.IsActive);
            Assert.NotNull(saved.DeletedAt);
        }

        [Fact]
        public async Task Reinstate_SuspendedUser_StillWorks()
        {
            using var ctx = CreateContext(nameof(Reinstate_SuspendedUser_StillWorks));
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com");
            await CreateController(ctx).SuspendUser(user.Id);

            var result = await CreateController(ctx).ReinstateUser(user.Id);

            Assert.IsType<OkObjectResult>(result);
            Assert.True((await ctx.Users.FindAsync(user.Id))!.IsActive);
        }

        [Fact]
        public async Task Suspend_DeletedUser_Returns404()
        {
            using var ctx = CreateContext(nameof(Suspend_DeletedUser_Returns404));
            var user = await SeedUser(ctx, UserRole.Client, "a@example.com", isActive: false, deletedAt: DateTime.UtcNow);

            Assert.IsType<NotFoundObjectResult>(await CreateController(ctx).SuspendUser(user.Id));
        }

        [Fact]
        public async Task Suspend_Yourself_Returns409()
        {
            using var ctx = CreateContext(nameof(Suspend_Yourself_Returns409));
            var me = await SeedUser(ctx, UserRole.Admin, "me@example.com", id: CallerId);
            await SeedUser(ctx, UserRole.Admin, "other@example.com");

            var result = await CreateController(ctx).SuspendUser(me.Id);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("You cannot suspend your own account. Ask another administrator.", ErrorBody(result).Message);
            Assert.True((await ctx.Users.FindAsync(me.Id))!.IsActive);
        }

        [Fact]
        public async Task Suspend_LastActiveAdmin_Returns409()
        {
            using var ctx = CreateContext(nameof(Suspend_LastActiveAdmin_Returns409));
            var admin = await SeedUser(ctx, UserRole.Admin, "only@example.com");

            var result = await CreateController(ctx).SuspendUser(admin.Id);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("last active administrator", ErrorBody(result).Message);
        }

        [Fact]
        public async Task Suspend_AdminWhenAnotherActiveAdminExists_Returns200()
        {
            using var ctx = CreateContext(nameof(Suspend_AdminWhenAnotherActiveAdminExists_Returns200));
            var admin = await SeedUser(ctx, UserRole.Admin, "a@example.com");
            await SeedUser(ctx, UserRole.Admin, "b@example.com");

            Assert.IsType<OkObjectResult>(await CreateController(ctx).SuspendUser(admin.Id));
        }

        [Fact]
        public async Task Suspend_ConsultantWithActiveCases_IsAllowed()
        {
            // Suspension is temporary (for example a compromised account), so it is not blocked by open cases.
            using var ctx = CreateContext(nameof(Suspend_ConsultantWithActiveCases_IsAllowed));
            var consultant = await SeedUser(ctx, UserRole.Consultant, "c@example.com");
            await SeedCase(ctx, consultant.Id, CaseStage.UnderReview);

            Assert.IsType<OkObjectResult>(await CreateController(ctx).SuspendUser(consultant.Id));
        }

        [Fact]
        public async Task Delete_Yourself_Returns409()
        {
            using var ctx = CreateContext(nameof(Delete_Yourself_Returns409));
            var me = await SeedUser(ctx, UserRole.Admin, "me@example.com", id: CallerId);
            await SeedUser(ctx, UserRole.Admin, "other@example.com");

            var result = await CreateController(ctx).DeleteUser(me.Id);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal("You cannot delete your own account. Ask another administrator.", ErrorBody(result).Message);
            Assert.Null((await ctx.Users.FindAsync(me.Id))!.DeletedAt);
        }

        [Fact]
        public async Task Delete_LastActiveAdmin_Returns409()
        {
            using var ctx = CreateContext(nameof(Delete_LastActiveAdmin_Returns409));
            var admin = await SeedUser(ctx, UserRole.Admin, "only@example.com");

            var result = await CreateController(ctx).DeleteUser(admin.Id);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Null((await ctx.Users.FindAsync(admin.Id))!.DeletedAt);
        }

        [Fact]
        public async Task Delete_ConsultantWithActiveCases_Returns409WithCount()
        {
            using var ctx = CreateContext(nameof(Delete_ConsultantWithActiveCases_Returns409WithCount));
            var consultant = await SeedUser(ctx, UserRole.Consultant, "c@example.com");
            await SeedCase(ctx, consultant.Id, CaseStage.UnderReview);
            await SeedCase(ctx, consultant.Id, CaseStage.FinalDecision);

            var result = await CreateController(ctx).DeleteUser(consultant.Id);

            Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(
                "This consultant has 1 active assigned case. Reassign them to another consultant before deleting the account.",
                ErrorBody(result).Message);
            Assert.Null((await ctx.Users.FindAsync(consultant.Id))!.DeletedAt);
        }

        [Fact]
        public async Task GetUsers_ExcludesDeletedButKeepsSuspended()
        {
            using var ctx = CreateContext(nameof(GetUsers_ExcludesDeletedButKeepsSuspended));
            await SeedUser(ctx, UserRole.Client, "active@example.com", "Active");
            await SeedUser(ctx, UserRole.Client, "suspended@example.com", "Suspended", isActive: false);
            await SeedUser(ctx, UserRole.Client, "deleted@example.com", "Deleted", isActive: false, deletedAt: DateTime.UtcNow);

            var result = await CreateController(ctx).GetUsers();

            var page = Assert.IsType<PagedResponse<UserDto>>(Assert.IsType<OkObjectResult>(result).Value);
            Assert.Equal(2, page.TotalRecords);
            Assert.Equal(new[] { "Active", "Suspended" }, page.Data.Select(u => u.FullName).OrderBy(n => n));
        }

        [Fact]
        public async Task GetUsers_ReportsAnActiveLockoutAndIgnoresOneThatHasEnded()
        {
            using var ctx = CreateContext(nameof(GetUsers_ReportsAnActiveLockoutAndIgnoresOneThatHasEnded));
            var until = DateTime.UtcNow.AddMinutes(10);
            var locked = await SeedUser(ctx, UserRole.Client, "locked@example.com", "Locked");
            locked.LockoutEndsAt = until;
            var expired = await SeedUser(ctx, UserRole.Client, "expired@example.com", "Expired");
            expired.LockoutEndsAt = DateTime.UtcNow.AddMinutes(-1);
            await SeedUser(ctx, UserRole.Client, "plain@example.com", "Plain");
            await ctx.SaveChangesAsync();

            var result = await CreateController(ctx).GetUsers();

            var users = Assert.IsType<PagedResponse<UserDto>>(Assert.IsType<OkObjectResult>(result).Value).Data.ToDictionary(u => u.FullName);
            Assert.True(users["Locked"].IsLocked);
            Assert.Equal(until, users["Locked"].LockoutEndsAt);
            Assert.False(users["Expired"].IsLocked);
            Assert.Null(users["Expired"].LockoutEndsAt);
            Assert.False(users["Plain"].IsLocked);
            Assert.Null(users["Plain"].LockoutEndsAt);
        }

        [Fact]
        public async Task GetUsers_ReturnsCreatedAtForEachUser()
        {
            using var ctx = CreateContext(nameof(GetUsers_ReturnsCreatedAtForEachUser));
            var createdTime = new DateTime(2026, 10, 15, 8, 30, 0, DateTimeKind.Utc);
            var user = await SeedUser(ctx, UserRole.Client, "alice@example.com", "Alice");
            user.CreatedAt = createdTime;
            await ctx.SaveChangesAsync();

            var result = await CreateController(ctx).GetUsers();

            var page = Assert.IsType<PagedResponse<UserDto>>(Assert.IsType<OkObjectResult>(result).Value);
            var alice = Assert.Single(page.Data);
            Assert.Equal(createdTime, alice.CreatedAt);
        }

        // ---------- Login ----------

        private static AuthService CreateAuthService(ApplicationDbContext ctx)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Jwt:Key",      "TestOnlyKey_MustBeAtLeast32CharactersLong!"),
                    new KeyValuePair<string, string?>("Jwt:Issuer",   "TestIssuer"),
                    new KeyValuePair<string, string?>("Jwt:Audience", "TestAudience"),
                    new KeyValuePair<string, string?>("Jwt:AccessTokenExpirationMinutes", "60"),
                    new KeyValuePair<string, string?>("Jwt:RefreshTokenExpirationDays",   "7"),
                })
                .Build();
            return new AuthService(ctx, new PasswordHasherService(), new TokenService(config),
                new InMemoryRefreshTokenStore(), NullLogger<AuthService>.Instance,
                new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
                Microsoft.Extensions.Options.Options.Create(new Strategies.Api.Configuration.LockoutSettings()), TimeProvider.System);
        }

        [Fact]
        public async Task Login_DeletedUser_IsRefused_EvenIfIsActiveWereTrue()
        {
            using var ctx = CreateContext(nameof(Login_DeletedUser_IsRefused_EvenIfIsActiveWereTrue));
            var hasher = new PasswordHasherService();
            var live = await SeedUser(ctx, UserRole.Client, "live@example.com");
            var gone = await SeedUser(ctx, UserRole.Client, "gone@example.com", deletedAt: DateTime.UtcNow);
            live.PasswordHash = hasher.HashPassword("Correct-Password-1");
            gone.PasswordHash = hasher.HashPassword("Correct-Password-1");
            await ctx.SaveChangesAsync();
            var svc = CreateAuthService(ctx);

            var liveLogin = await svc.LoginAsync(new LoginRequest { Email = "live@example.com", Password = "Correct-Password-1" });
            var goneLogin = await svc.LoginAsync(new LoginRequest { Email = "gone@example.com", Password = "Correct-Password-1" });

            Assert.NotNull(liveLogin);
            Assert.Null(goneLogin);
        }
    }
}
