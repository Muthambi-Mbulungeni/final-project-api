using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Strategies.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTriageReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_VisaRecords",
                table: "VisaRecords");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VisaCategories",
                table: "VisaCategories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Users",
                table: "Users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Messages",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Leads",
                table: "Leads");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocumentTypes",
                table: "DocumentTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Documents",
                table: "Documents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChecklistTemplates",
                table: "ChecklistTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CaseStageHistories",
                table: "CaseStageHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cases",
                table: "Cases");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AgentRuns",
                table: "AgentRuns");

            migrationBuilder.RenameTable(
                name: "VisaRecords",
                newName: "VisaRecord");

            migrationBuilder.RenameTable(
                name: "VisaCategories",
                newName: "VisaCategory");

            migrationBuilder.RenameTable(
                name: "Users",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "Messages",
                newName: "Message");

            migrationBuilder.RenameTable(
                name: "Leads",
                newName: "Lead");

            migrationBuilder.RenameTable(
                name: "DocumentTypes",
                newName: "DocumentType");

            migrationBuilder.RenameTable(
                name: "Documents",
                newName: "Document");

            migrationBuilder.RenameTable(
                name: "ChecklistTemplates",
                newName: "ChecklistTemplate");

            migrationBuilder.RenameTable(
                name: "CaseStageHistories",
                newName: "CaseStageHistory");

            migrationBuilder.RenameTable(
                name: "Cases",
                newName: "Case");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "AuditLog");

            migrationBuilder.RenameTable(
                name: "AgentRuns",
                newName: "AgentRun");

            migrationBuilder.AlterColumn<bool>(
                name: "RenewalFlagged",
                table: "VisaRecord",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VisaCategory",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "VisaCategory",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "User",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "Message",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRead",
                table: "Message",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "NeedsAssistance",
                table: "Lead",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<string>(
                name: "TriageReason",
                table: "Lead",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DocumentType",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadedAt",
                table: "Document",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewComment",
                table: "Document",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsArchived",
                table: "Document",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsMandatory",
                table: "ChecklistTemplate",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "CaseStageHistory",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Case",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<bool>(
                name: "IsArchived",
                table: "Case",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Case",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AuditLog",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RanAt",
                table: "AgentRun",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VisaRecord",
                table: "VisaRecord",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VisaCategory",
                table: "VisaCategory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Message",
                table: "Message",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Lead",
                table: "Lead",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocumentType",
                table: "DocumentType",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Document",
                table: "Document",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChecklistTemplate",
                table: "ChecklistTemplate",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CaseStageHistory",
                table: "CaseStageHistory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Case",
                table: "Case",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLog",
                table: "AuditLog",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AgentRun",
                table: "AgentRun",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_VisaRecord_CaseId",
                table: "VisaRecord",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_CaseId",
                table: "Message",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Message_SenderId",
                table: "Message",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Lead_ReviewedById",
                table: "Lead",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_Lead_VisaCategoryId",
                table: "Lead",
                column: "VisaCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_CaseId",
                table: "Document",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_DocumentTypeId",
                table: "Document",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Document_ReviewedById",
                table: "Document",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplate_DocumentTypeId",
                table: "ChecklistTemplate",
                column: "DocumentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistTemplate_VisaCategoryId",
                table: "ChecklistTemplate",
                column: "VisaCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseStageHistory_CaseId",
                table: "CaseStageHistory",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CaseStageHistory_ChangedById",
                table: "CaseStageHistory",
                column: "ChangedById");

            migrationBuilder.CreateIndex(
                name: "IX_Case_ClientId",
                table: "Case",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Case_ConsultantId",
                table: "Case",
                column: "ConsultantId");

            migrationBuilder.CreateIndex(
                name: "IX_Case_LeadId",
                table: "Case",
                column: "LeadId");

            migrationBuilder.CreateIndex(
                name: "IX_Case_VisaCategoryId",
                table: "Case",
                column: "VisaCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLog",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRun_CaseId",
                table: "AgentRun",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRun_LeadId",
                table: "AgentRun",
                column: "LeadId");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRun_Case_CaseId",
                table: "AgentRun",
                column: "CaseId",
                principalTable: "Case",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AgentRun_Lead_LeadId",
                table: "AgentRun",
                column: "LeadId",
                principalTable: "Lead",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLog_User_UserId",
                table: "AuditLog",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Case_Lead_LeadId",
                table: "Case",
                column: "LeadId",
                principalTable: "Lead",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Case_User_ClientId",
                table: "Case",
                column: "ClientId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Case_User_ConsultantId",
                table: "Case",
                column: "ConsultantId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Case_VisaCategory_VisaCategoryId",
                table: "Case",
                column: "VisaCategoryId",
                principalTable: "VisaCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseStageHistory_Case_CaseId",
                table: "CaseStageHistory",
                column: "CaseId",
                principalTable: "Case",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CaseStageHistory_User_ChangedById",
                table: "CaseStageHistory",
                column: "ChangedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ChecklistTemplate_DocumentType_DocumentTypeId",
                table: "ChecklistTemplate",
                column: "DocumentTypeId",
                principalTable: "DocumentType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ChecklistTemplate_VisaCategory_VisaCategoryId",
                table: "ChecklistTemplate",
                column: "VisaCategoryId",
                principalTable: "VisaCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Document_Case_CaseId",
                table: "Document",
                column: "CaseId",
                principalTable: "Case",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Document_DocumentType_DocumentTypeId",
                table: "Document",
                column: "DocumentTypeId",
                principalTable: "DocumentType",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Document_User_ReviewedById",
                table: "Document",
                column: "ReviewedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Lead_User_ReviewedById",
                table: "Lead",
                column: "ReviewedById",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Lead_VisaCategory_VisaCategoryId",
                table: "Lead",
                column: "VisaCategoryId",
                principalTable: "VisaCategory",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Message_Case_CaseId",
                table: "Message",
                column: "CaseId",
                principalTable: "Case",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Message_User_SenderId",
                table: "Message",
                column: "SenderId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VisaRecord_Case_CaseId",
                table: "VisaRecord",
                column: "CaseId",
                principalTable: "Case",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentRun_Case_CaseId",
                table: "AgentRun");

            migrationBuilder.DropForeignKey(
                name: "FK_AgentRun_Lead_LeadId",
                table: "AgentRun");

            migrationBuilder.DropForeignKey(
                name: "FK_AuditLog_User_UserId",
                table: "AuditLog");

            migrationBuilder.DropForeignKey(
                name: "FK_Case_Lead_LeadId",
                table: "Case");

            migrationBuilder.DropForeignKey(
                name: "FK_Case_User_ClientId",
                table: "Case");

            migrationBuilder.DropForeignKey(
                name: "FK_Case_User_ConsultantId",
                table: "Case");

            migrationBuilder.DropForeignKey(
                name: "FK_Case_VisaCategory_VisaCategoryId",
                table: "Case");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseStageHistory_Case_CaseId",
                table: "CaseStageHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_CaseStageHistory_User_ChangedById",
                table: "CaseStageHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_ChecklistTemplate_DocumentType_DocumentTypeId",
                table: "ChecklistTemplate");

            migrationBuilder.DropForeignKey(
                name: "FK_ChecklistTemplate_VisaCategory_VisaCategoryId",
                table: "ChecklistTemplate");

            migrationBuilder.DropForeignKey(
                name: "FK_Document_Case_CaseId",
                table: "Document");

            migrationBuilder.DropForeignKey(
                name: "FK_Document_DocumentType_DocumentTypeId",
                table: "Document");

            migrationBuilder.DropForeignKey(
                name: "FK_Document_User_ReviewedById",
                table: "Document");

            migrationBuilder.DropForeignKey(
                name: "FK_Lead_User_ReviewedById",
                table: "Lead");

            migrationBuilder.DropForeignKey(
                name: "FK_Lead_VisaCategory_VisaCategoryId",
                table: "Lead");

            migrationBuilder.DropForeignKey(
                name: "FK_Message_Case_CaseId",
                table: "Message");

            migrationBuilder.DropForeignKey(
                name: "FK_Message_User_SenderId",
                table: "Message");

            migrationBuilder.DropForeignKey(
                name: "FK_VisaRecord_Case_CaseId",
                table: "VisaRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VisaRecord",
                table: "VisaRecord");

            migrationBuilder.DropIndex(
                name: "IX_VisaRecord_CaseId",
                table: "VisaRecord");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VisaCategory",
                table: "VisaCategory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Message",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_Message_CaseId",
                table: "Message");

            migrationBuilder.DropIndex(
                name: "IX_Message_SenderId",
                table: "Message");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Lead",
                table: "Lead");

            migrationBuilder.DropIndex(
                name: "IX_Lead_ReviewedById",
                table: "Lead");

            migrationBuilder.DropIndex(
                name: "IX_Lead_VisaCategoryId",
                table: "Lead");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DocumentType",
                table: "DocumentType");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Document",
                table: "Document");

            migrationBuilder.DropIndex(
                name: "IX_Document_CaseId",
                table: "Document");

            migrationBuilder.DropIndex(
                name: "IX_Document_DocumentTypeId",
                table: "Document");

            migrationBuilder.DropIndex(
                name: "IX_Document_ReviewedById",
                table: "Document");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ChecklistTemplate",
                table: "ChecklistTemplate");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistTemplate_DocumentTypeId",
                table: "ChecklistTemplate");

            migrationBuilder.DropIndex(
                name: "IX_ChecklistTemplate_VisaCategoryId",
                table: "ChecklistTemplate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CaseStageHistory",
                table: "CaseStageHistory");

            migrationBuilder.DropIndex(
                name: "IX_CaseStageHistory_CaseId",
                table: "CaseStageHistory");

            migrationBuilder.DropIndex(
                name: "IX_CaseStageHistory_ChangedById",
                table: "CaseStageHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Case",
                table: "Case");

            migrationBuilder.DropIndex(
                name: "IX_Case_ClientId",
                table: "Case");

            migrationBuilder.DropIndex(
                name: "IX_Case_ConsultantId",
                table: "Case");

            migrationBuilder.DropIndex(
                name: "IX_Case_LeadId",
                table: "Case");

            migrationBuilder.DropIndex(
                name: "IX_Case_VisaCategoryId",
                table: "Case");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLog",
                table: "AuditLog");

            migrationBuilder.DropIndex(
                name: "IX_AuditLog_UserId",
                table: "AuditLog");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AgentRun",
                table: "AgentRun");

            migrationBuilder.DropIndex(
                name: "IX_AgentRun_CaseId",
                table: "AgentRun");

            migrationBuilder.DropIndex(
                name: "IX_AgentRun_LeadId",
                table: "AgentRun");

            migrationBuilder.DropColumn(
                name: "TriageReason",
                table: "Lead");

            migrationBuilder.RenameTable(
                name: "VisaRecord",
                newName: "VisaRecords");

            migrationBuilder.RenameTable(
                name: "VisaCategory",
                newName: "VisaCategories");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "Users");

            migrationBuilder.RenameTable(
                name: "Message",
                newName: "Messages");

            migrationBuilder.RenameTable(
                name: "Lead",
                newName: "Leads");

            migrationBuilder.RenameTable(
                name: "DocumentType",
                newName: "DocumentTypes");

            migrationBuilder.RenameTable(
                name: "Document",
                newName: "Documents");

            migrationBuilder.RenameTable(
                name: "ChecklistTemplate",
                newName: "ChecklistTemplates");

            migrationBuilder.RenameTable(
                name: "CaseStageHistory",
                newName: "CaseStageHistories");

            migrationBuilder.RenameTable(
                name: "Case",
                newName: "Cases");

            migrationBuilder.RenameTable(
                name: "AuditLog",
                newName: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "AgentRun",
                newName: "AgentRuns");

            migrationBuilder.AlterColumn<bool>(
                name: "RenewalFlagged",
                table: "VisaRecords",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "VisaCategories",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "VisaCategories",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "EmailConfirmed",
                table: "Users",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "SentAt",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<bool>(
                name: "IsRead",
                table: "Messages",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "NeedsAssistance",
                table: "Leads",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "DocumentTypes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "UploadedAt",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<string>(
                name: "ReviewComment",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsArchived",
                table: "Documents",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsMandatory",
                table: "ChecklistTemplates",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ChangedAt",
                table: "CaseStageHistories",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<bool>(
                name: "IsArchived",
                table: "Cases",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "RanAt",
                table: "AgentRuns",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VisaRecords",
                table: "VisaRecords",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VisaCategories",
                table: "VisaCategories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Users",
                table: "Users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Messages",
                table: "Messages",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Leads",
                table: "Leads",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DocumentTypes",
                table: "DocumentTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Documents",
                table: "Documents",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ChecklistTemplates",
                table: "ChecklistTemplates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CaseStageHistories",
                table: "CaseStageHistories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cases",
                table: "Cases",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AgentRuns",
                table: "AgentRuns",
                column: "Id");
        }
    }
}
