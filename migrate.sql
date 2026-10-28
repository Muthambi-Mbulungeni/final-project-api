CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "AgentRuns" (
        "Id" uuid NOT NULL,
        "CaseId" uuid,
        "LeadId" uuid,
        "AgentType" integer NOT NULL,
        "Output" text NOT NULL,
        "Status" integer NOT NULL,
        "RanAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AgentRuns" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "AuditLogs" (
        "Id" uuid NOT NULL,
        "UserId" uuid NOT NULL,
        "Action" text NOT NULL,
        "EntityType" text NOT NULL,
        "EntityId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "Cases" (
        "Id" uuid NOT NULL,
        "LeadId" uuid,
        "ClientId" uuid NOT NULL,
        "ConsultantId" uuid,
        "VisaCategoryId" uuid NOT NULL,
        "Stage" integer NOT NULL,
        "IsArchived" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Cases" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "CaseStageHistories" (
        "Id" uuid NOT NULL,
        "CaseId" uuid NOT NULL,
        "ChangedById" uuid NOT NULL,
        "FromStage" integer NOT NULL,
        "ToStage" integer NOT NULL,
        "ChangedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CaseStageHistories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "ChecklistTemplates" (
        "Id" uuid NOT NULL,
        "VisaCategoryId" uuid NOT NULL,
        "DocumentTypeId" uuid NOT NULL,
        "IsMandatory" boolean NOT NULL,
        CONSTRAINT "PK_ChecklistTemplates" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "Documents" (
        "Id" uuid NOT NULL,
        "CaseId" uuid NOT NULL,
        "DocumentTypeId" uuid NOT NULL,
        "ReviewedById" uuid,
        "FileName" text NOT NULL,
        "FilePath" text NOT NULL,
        "Status" integer NOT NULL,
        "ReviewComment" text NOT NULL,
        "UploadedAt" timestamp with time zone NOT NULL,
        "IsArchived" boolean NOT NULL,
        CONSTRAINT "PK_Documents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "DocumentTypes" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text NOT NULL,
        CONSTRAINT "PK_DocumentTypes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "Leads" (
        "Id" uuid NOT NULL,
        "ReviewedById" uuid,
        "VisaCategoryId" uuid,
        "FullName" text NOT NULL,
        "Email" text NOT NULL,
        "ImmigrationStatus" integer NOT NULL,
        "NeedsAssistance" boolean NOT NULL,
        "DesiredStartDate" timestamp with time zone NOT NULL,
        "TriageStatus" integer NOT NULL,
        "SubmittedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Leads" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "Messages" (
        "Id" uuid NOT NULL,
        "CaseId" uuid NOT NULL,
        "SenderId" uuid NOT NULL,
        "Body" text NOT NULL,
        "IsRead" boolean NOT NULL,
        "SentAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Messages" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "FullName" text NOT NULL,
        "Email" text NOT NULL,
        "PasswordHash" text NOT NULL,
        "Role" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        "EmailConfirmed" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "VisaCategories" (
        "Id" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text NOT NULL,
        "IsActive" boolean NOT NULL,
        CONSTRAINT "PK_VisaCategories" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    CREATE TABLE "VisaRecords" (
        "Id" uuid NOT NULL,
        "CaseId" uuid NOT NULL,
        "VisaType" text NOT NULL,
        "IssuedDate" timestamp with time zone NOT NULL,
        "ExpiryDate" timestamp with time zone NOT NULL,
        "RenewalFlagged" boolean NOT NULL,
        CONSTRAINT "PK_VisaRecords" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260927145405_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260927145405_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaRecords" DROP CONSTRAINT "PK_VisaRecords";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaCategories" DROP CONSTRAINT "PK_VisaCategories";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Users" DROP CONSTRAINT "PK_Users";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Messages" DROP CONSTRAINT "PK_Messages";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Leads" DROP CONSTRAINT "PK_Leads";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "DocumentTypes" DROP CONSTRAINT "PK_DocumentTypes";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Documents" DROP CONSTRAINT "PK_Documents";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "ChecklistTemplates" DROP CONSTRAINT "PK_ChecklistTemplates";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "CaseStageHistories" DROP CONSTRAINT "PK_CaseStageHistories";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Cases" DROP CONSTRAINT "PK_Cases";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AuditLogs" DROP CONSTRAINT "PK_AuditLogs";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AgentRuns" DROP CONSTRAINT "PK_AgentRuns";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaRecords" RENAME TO "VisaRecord";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaCategories" RENAME TO "VisaCategory";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Users" RENAME TO "User";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Messages" RENAME TO "Message";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Leads" RENAME TO "Lead";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "DocumentTypes" RENAME TO "DocumentType";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Documents" RENAME TO "Document";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "ChecklistTemplates" RENAME TO "ChecklistTemplate";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "CaseStageHistories" RENAME TO "CaseStageHistory";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Cases" RENAME TO "Case";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AuditLogs" RENAME TO "AuditLog";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AgentRuns" RENAME TO "AgentRun";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaRecord" ALTER COLUMN "RenewalFlagged" SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaCategory" ALTER COLUMN "IsActive" SET DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaCategory" ALTER COLUMN "Description" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "User" ALTER COLUMN "IsActive" SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "User" ALTER COLUMN "EmailConfirmed" SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "User" ALTER COLUMN "CreatedAt" SET DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Message" ALTER COLUMN "SentAt" SET DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Message" ALTER COLUMN "IsRead" SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Lead" ALTER COLUMN "NeedsAssistance" SET DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Lead" ADD "TriageReason" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "DocumentType" ALTER COLUMN "Description" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Document" ALTER COLUMN "UploadedAt" SET DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Document" ALTER COLUMN "ReviewComment" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Document" ALTER COLUMN "IsArchived" SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "ChecklistTemplate" ALTER COLUMN "IsMandatory" SET DEFAULT TRUE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "CaseStageHistory" ALTER COLUMN "ChangedAt" SET DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Case" ALTER COLUMN "UpdatedAt" SET DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Case" ALTER COLUMN "IsArchived" SET DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Case" ALTER COLUMN "CreatedAt" SET DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AuditLog" ALTER COLUMN "CreatedAt" SET DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AgentRun" ALTER COLUMN "RanAt" SET DEFAULT (now());
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaRecord" ADD CONSTRAINT "PK_VisaRecord" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaCategory" ADD CONSTRAINT "PK_VisaCategory" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "User" ADD CONSTRAINT "PK_User" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Message" ADD CONSTRAINT "PK_Message" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Lead" ADD CONSTRAINT "PK_Lead" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "DocumentType" ADD CONSTRAINT "PK_DocumentType" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Document" ADD CONSTRAINT "PK_Document" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "ChecklistTemplate" ADD CONSTRAINT "PK_ChecklistTemplate" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "CaseStageHistory" ADD CONSTRAINT "PK_CaseStageHistory" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Case" ADD CONSTRAINT "PK_Case" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AuditLog" ADD CONSTRAINT "PK_AuditLog" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AgentRun" ADD CONSTRAINT "PK_AgentRun" PRIMARY KEY ("Id");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_VisaRecord_CaseId" ON "VisaRecord" ("CaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Message_CaseId" ON "Message" ("CaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Message_SenderId" ON "Message" ("SenderId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Lead_ReviewedById" ON "Lead" ("ReviewedById");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Lead_VisaCategoryId" ON "Lead" ("VisaCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Document_CaseId" ON "Document" ("CaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Document_DocumentTypeId" ON "Document" ("DocumentTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Document_ReviewedById" ON "Document" ("ReviewedById");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_ChecklistTemplate_DocumentTypeId" ON "ChecklistTemplate" ("DocumentTypeId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_ChecklistTemplate_VisaCategoryId" ON "ChecklistTemplate" ("VisaCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_CaseStageHistory_CaseId" ON "CaseStageHistory" ("CaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_CaseStageHistory_ChangedById" ON "CaseStageHistory" ("ChangedById");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Case_ClientId" ON "Case" ("ClientId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Case_ConsultantId" ON "Case" ("ConsultantId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Case_LeadId" ON "Case" ("LeadId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_Case_VisaCategoryId" ON "Case" ("VisaCategoryId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_AuditLog_UserId" ON "AuditLog" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_AgentRun_CaseId" ON "AgentRun" ("CaseId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    CREATE INDEX "IX_AgentRun_LeadId" ON "AgentRun" ("LeadId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AgentRun" ADD CONSTRAINT "FK_AgentRun_Case_CaseId" FOREIGN KEY ("CaseId") REFERENCES "Case" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AgentRun" ADD CONSTRAINT "FK_AgentRun_Lead_LeadId" FOREIGN KEY ("LeadId") REFERENCES "Lead" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "AuditLog" ADD CONSTRAINT "FK_AuditLog_User_UserId" FOREIGN KEY ("UserId") REFERENCES "User" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Case" ADD CONSTRAINT "FK_Case_Lead_LeadId" FOREIGN KEY ("LeadId") REFERENCES "Lead" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Case" ADD CONSTRAINT "FK_Case_User_ClientId" FOREIGN KEY ("ClientId") REFERENCES "User" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Case" ADD CONSTRAINT "FK_Case_User_ConsultantId" FOREIGN KEY ("ConsultantId") REFERENCES "User" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Case" ADD CONSTRAINT "FK_Case_VisaCategory_VisaCategoryId" FOREIGN KEY ("VisaCategoryId") REFERENCES "VisaCategory" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "CaseStageHistory" ADD CONSTRAINT "FK_CaseStageHistory_Case_CaseId" FOREIGN KEY ("CaseId") REFERENCES "Case" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "CaseStageHistory" ADD CONSTRAINT "FK_CaseStageHistory_User_ChangedById" FOREIGN KEY ("ChangedById") REFERENCES "User" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "ChecklistTemplate" ADD CONSTRAINT "FK_ChecklistTemplate_DocumentType_DocumentTypeId" FOREIGN KEY ("DocumentTypeId") REFERENCES "DocumentType" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "ChecklistTemplate" ADD CONSTRAINT "FK_ChecklistTemplate_VisaCategory_VisaCategoryId" FOREIGN KEY ("VisaCategoryId") REFERENCES "VisaCategory" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Document" ADD CONSTRAINT "FK_Document_Case_CaseId" FOREIGN KEY ("CaseId") REFERENCES "Case" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Document" ADD CONSTRAINT "FK_Document_DocumentType_DocumentTypeId" FOREIGN KEY ("DocumentTypeId") REFERENCES "DocumentType" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Document" ADD CONSTRAINT "FK_Document_User_ReviewedById" FOREIGN KEY ("ReviewedById") REFERENCES "User" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Lead" ADD CONSTRAINT "FK_Lead_User_ReviewedById" FOREIGN KEY ("ReviewedById") REFERENCES "User" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Lead" ADD CONSTRAINT "FK_Lead_VisaCategory_VisaCategoryId" FOREIGN KEY ("VisaCategoryId") REFERENCES "VisaCategory" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Message" ADD CONSTRAINT "FK_Message_Case_CaseId" FOREIGN KEY ("CaseId") REFERENCES "Case" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "Message" ADD CONSTRAINT "FK_Message_User_SenderId" FOREIGN KEY ("SenderId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    ALTER TABLE "VisaRecord" ADD CONSTRAINT "FK_VisaRecord_Case_CaseId" FOREIGN KEY ("CaseId") REFERENCES "Case" ("Id") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261003134749_AddTriageReason') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261003134749_AddTriageReason', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004065713_FixDeleteBehaviors') THEN
    ALTER TABLE "Case" DROP CONSTRAINT "FK_Case_User_ClientId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004065713_FixDeleteBehaviors') THEN
    ALTER TABLE "Case" DROP CONSTRAINT "FK_Case_VisaCategory_VisaCategoryId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004065713_FixDeleteBehaviors') THEN
    ALTER TABLE "CaseStageHistory" ALTER COLUMN "ChangedById" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004065713_FixDeleteBehaviors') THEN
    ALTER TABLE "AuditLog" ALTER COLUMN "UserId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004065713_FixDeleteBehaviors') THEN
    ALTER TABLE "Case" ADD CONSTRAINT "FK_Case_User_ClientId" FOREIGN KEY ("ClientId") REFERENCES "User" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004065713_FixDeleteBehaviors') THEN
    ALTER TABLE "Case" ADD CONSTRAINT "FK_Case_VisaCategory_VisaCategoryId" FOREIGN KEY ("VisaCategoryId") REFERENCES "VisaCategory" ("Id") ON DELETE RESTRICT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261004065713_FixDeleteBehaviors') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261004065713_FixDeleteBehaviors', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
        IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'databasestorage') THEN
            CREATE SCHEMA databasestorage;
        END IF;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "VisaRecord" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "VisaCategory" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "User" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "Message" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "Lead" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "DocumentType" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "Document" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "ChecklistTemplate" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "CaseStageHistory" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "Case" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "AuditLog" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    ALTER TABLE "AgentRun" SET SCHEMA databasestorage;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005160408_SBMigration') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261005160408_SBMigration', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."VisaRecord" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."VisaCategory" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."User" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."Message" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."Lead" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."DocumentType" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."Document" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."ChecklistTemplate" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."CaseStageHistory" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."Case" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."AuditLog" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    ALTER TABLE databasestorage."AgentRun" SET SCHEMA public;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261005182839_RevertSchemaToPublic') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261005182839_RevertSchemaToPublic', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022095911_AddUserDeletedAt') THEN
    ALTER TABLE public."User" ADD "DeletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022095911_AddUserDeletedAt') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261022095911_AddUserDeletedAt', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022140013_AddDocumentReviewedAt') THEN
    ALTER TABLE public."Document" ADD "ReviewedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261022140013_AddDocumentReviewedAt') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261022140013_AddDocumentReviewedAt', '10.0.12');
    END IF;
END $EF$;
COMMIT;

