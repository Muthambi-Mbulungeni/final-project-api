using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Strategies.Api.Migrations
{
    /// <inheritdoc />
    public partial class SBMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "databasestorage");

            migrationBuilder.RenameTable(
                name: "VisaRecord",
                newName: "VisaRecord",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "VisaCategory",
                newName: "VisaCategory",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "User",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "Message",
                newName: "Message",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "Lead",
                newName: "Lead",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "DocumentType",
                newName: "DocumentType",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "Document",
                newName: "Document",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "ChecklistTemplate",
                newName: "ChecklistTemplate",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "CaseStageHistory",
                newName: "CaseStageHistory",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "Case",
                newName: "Case",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                newName: "AuditLog",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "AgentRun",
                newName: "AgentRun",
                newSchema: "databasestorage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "VisaRecord",
                schema: "databasestorage",
                newName: "VisaRecord");

            migrationBuilder.RenameTable(
                name: "VisaCategory",
                schema: "databasestorage",
                newName: "VisaCategory");

            migrationBuilder.RenameTable(
                name: "User",
                schema: "databasestorage",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "Message",
                schema: "databasestorage",
                newName: "Message");

            migrationBuilder.RenameTable(
                name: "Lead",
                schema: "databasestorage",
                newName: "Lead");

            migrationBuilder.RenameTable(
                name: "DocumentType",
                schema: "databasestorage",
                newName: "DocumentType");

            migrationBuilder.RenameTable(
                name: "Document",
                schema: "databasestorage",
                newName: "Document");

            migrationBuilder.RenameTable(
                name: "ChecklistTemplate",
                schema: "databasestorage",
                newName: "ChecklistTemplate");

            migrationBuilder.RenameTable(
                name: "CaseStageHistory",
                schema: "databasestorage",
                newName: "CaseStageHistory");

            migrationBuilder.RenameTable(
                name: "Case",
                schema: "databasestorage",
                newName: "Case");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                schema: "databasestorage",
                newName: "AuditLog");

            migrationBuilder.RenameTable(
                name: "AgentRun",
                schema: "databasestorage",
                newName: "AgentRun");
        }
    }
}
