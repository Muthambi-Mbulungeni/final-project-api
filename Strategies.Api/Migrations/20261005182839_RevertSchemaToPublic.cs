using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Strategies.Api.Migrations
{
    /// <inheritdoc />
    public partial class RevertSchemaToPublic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.RenameTable(
                name: "VisaRecord",
                schema: "databasestorage",
                newName: "VisaRecord",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "VisaCategory",
                schema: "databasestorage",
                newName: "VisaCategory",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "User",
                schema: "databasestorage",
                newName: "User",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Message",
                schema: "databasestorage",
                newName: "Message",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Lead",
                schema: "databasestorage",
                newName: "Lead",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "DocumentType",
                schema: "databasestorage",
                newName: "DocumentType",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Document",
                schema: "databasestorage",
                newName: "Document",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "ChecklistTemplate",
                schema: "databasestorage",
                newName: "ChecklistTemplate",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "CaseStageHistory",
                schema: "databasestorage",
                newName: "CaseStageHistory",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "Case",
                schema: "databasestorage",
                newName: "Case",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                schema: "databasestorage",
                newName: "AuditLog",
                newSchema: "public");

            migrationBuilder.RenameTable(
                name: "AgentRun",
                schema: "databasestorage",
                newName: "AgentRun",
                newSchema: "public");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "databasestorage");

            migrationBuilder.RenameTable(
                name: "VisaRecord",
                schema: "public",
                newName: "VisaRecord",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "VisaCategory",
                schema: "public",
                newName: "VisaCategory",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "User",
                schema: "public",
                newName: "User",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "Message",
                schema: "public",
                newName: "Message",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "Lead",
                schema: "public",
                newName: "Lead",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "DocumentType",
                schema: "public",
                newName: "DocumentType",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "Document",
                schema: "public",
                newName: "Document",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "ChecklistTemplate",
                schema: "public",
                newName: "ChecklistTemplate",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "CaseStageHistory",
                schema: "public",
                newName: "CaseStageHistory",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "Case",
                schema: "public",
                newName: "Case",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                schema: "public",
                newName: "AuditLog",
                newSchema: "databasestorage");

            migrationBuilder.RenameTable(
                name: "AgentRun",
                schema: "public",
                newName: "AgentRun",
                newSchema: "databasestorage");
        }
    }
}
