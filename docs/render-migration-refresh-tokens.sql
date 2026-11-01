-- Render database: creates the RefreshToken table for PR #42 (ISS-15).
-- Run once before or when #42 is merged. Safe to run again: every step checks
-- __EFMigrationsHistory first. The same block is at the end of migrate.sql.

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261029132529_AddRefreshTokenTable') THEN
    CREATE TABLE public."RefreshToken" (
        "Id" uuid NOT NULL,
        "TokenHash" character varying(64) NOT NULL,
        "UserId" uuid NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL DEFAULT (now()),
        "ExpiresAt" timestamp with time zone NOT NULL,
        "IsRevoked" boolean NOT NULL DEFAULT FALSE,
        "RevokedAt" timestamp with time zone,
        "ReplacedByTokenHash" character varying(64),
        CONSTRAINT "PK_RefreshToken" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_RefreshToken_User_UserId" FOREIGN KEY ("UserId") REFERENCES public."User" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261029132529_AddRefreshTokenTable') THEN
    CREATE UNIQUE INDEX "IX_RefreshToken_TokenHash" ON public."RefreshToken" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261029132529_AddRefreshTokenTable') THEN
    CREATE INDEX "IX_RefreshToken_UserId" ON public."RefreshToken" ("UserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261029132529_AddRefreshTokenTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261029132529_AddRefreshTokenTable', '10.0.12');
    END IF;
END $EF$;
COMMIT;

