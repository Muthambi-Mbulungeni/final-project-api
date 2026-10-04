# Seed data (demo)

Demo data for the Strategies Migration Platform. It matches the schema created by the EF Core
migrations in `Strategies.Api` (`InitialCreate`, `AddTriageReason`, `FixDeleteBehaviors`): singular
table names in the `public` schema. Apply the migrations first, then run the two files in order.

Ids are fixed, so re-running gives the same rows. The four dev accounts keep the API's built-in ids;
every other id is distinct, so the 8-character references the portal shows differ between records.

> **Warning:** file 1 starts with `TRUNCATE ... CASCADE` on every application table. Running it
> wipes all existing data in those tables. Use it on demo databases only.

## What it creates

| File | Contents |
|---|---|
| `1-reference-and-users.sql` | 11 users (Admin, Consultant and Client roles), 5 visa categories, 5 document types |
| `2-cases-and-activity.sql` | 11 leads across every triage status, 7 cases across all 5 lifecycle stages, 6 stage-history rows, 5 documents (pending, approved, rejected), 6 messages, 10 audit-log rows |

Passwords are hashed in the database with bcrypt via pgcrypto (`crypt(..., gen_salt('bf', 11))`),
which the API's BCrypt verification accepts.

## Dev accounts (demo only)

| Role | Email | Password |
|---|---|---|
| Admin | admin@nexus.com | AdminPassword123! |
| Consultant | consultant@nexus.com | ConsultantPassword123! |
| Client | client@nexus.com | ClientPassword123! |

The other seeded consultants and clients share their role's password. These accounts exist for
demonstration only and must never be used on a system holding real client data.

## Run locally (Docker)

From the repository root, with the API's `DefaultConnection` pointing at the local container:

```bash
cd Strategies.Api && dotnet ef database update && cd ..
docker exec -i smp-postgres psql -U postgres -d strategies -v ON_ERROR_STOP=1 < database/seed/1-reference-and-users.sql
docker exec -i smp-postgres psql -U postgres -d strategies -v ON_ERROR_STOP=1 < database/seed/2-cases-and-activity.sql
```

## Run on Supabase

1. From a terminal in `Strategies.Api`, with `DefaultConnection` set to the Supabase session pooler
   string in .NET User Secrets: `dotnet ef database update`
2. In the Supabase SQL Editor, paste and run file 1, then file 2.
3. If the editor reports that `gen_salt` or `crypt` does not exist, pgcrypto is not on the search
   path. Run `SET search_path TO public, extensions;` in the same editor tab, then run the file again.
4. Verify:

```sql
SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY 1;
SELECT count(*) FROM public."User";
```

Expected: three migrations and 11 users.
