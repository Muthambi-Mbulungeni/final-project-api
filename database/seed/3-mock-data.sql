-- DEMO DATA ONLY. Run AFTER 1-reference-and-users.sql and 2-cases-and-activity.sql.
-- Adds extra mock data on top of them. It does NOT truncate anything and is safe to re-run
-- (every row has a fixed id and uses ON CONFLICT DO NOTHING).
-- Enums are integers:
--   UserRole: Admin=0 Consultant=1 Client=2
--   ImmigrationStatus: Citizen=0 PermanentResident=1 TemporaryResident=2 Foreigner=3
--   TriageStatus: Pending=0 Qualified=1 Unqualified=2 NeedsReview=3
--   CaseStage: InitialIntake=0 DocumentGathering=1 UnderReview=2 SubmittedToAuthority=3 FinalDecision=4
--   DocumentStatus: Pending=0 Approved=1 Rejected=2
--   AgentType: Triage=0   RunStatus: Success=0 Fallback=1 Error=2
-- Mock accounts: mock.consultant1..3@example.com (ConsultantPassword123!),
--   mock.client01..15@example.com (ClientPassword123!), plus a suspended, a deleted and a locked-out client.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

DO $$
DECLARE
  admin_id    uuid := '00000000-0000-0000-0000-000000000001';
  pw_client   text := crypt('ClientPassword123!', gen_salt('bf', 11));
  pw_cons     text := crypt('ConsultantPassword123!', gen_salt('bf', 11));

  -- Active consultants: the 2 from the base seed + 3 new ones.
  consultants uuid[] := ARRAY[
    '33333333-3333-3333-3333-333333333334',
    '33333333-3333-3333-3333-333333333333',
    md5('s3-cons-1'), md5('s3-cons-2'), md5('s3-cons-3')]::uuid[];

  visa_ids    uuid[] := ARRAY[
    '4822fd67-4407-448f-aed6-bb6997f2e849','2c0239e6-8648-4ba9-9f9b-e8e614fd2e05',
    '35b7aa88-ff7f-4a3c-9489-8f176c705c76','29542358-d41e-4e4f-9340-2a0d0bb22ae6',
    '35f3c178-f925-4725-941f-ef0cf10761ae']::uuid[];
  visa_names  text[] := ARRAY['Critical Skills Work Visa','General Work Visa','Spousal Visa','Study Visa','Permanent Residence'];
  doc_ids     uuid[] := ARRAY[
    'd29caa63-37df-4fe6-98ca-b1cc49550469','c6860be5-e0d7-452a-a248-56796622a648',
    'f6a454ac-d6be-47b8-bd54-626654e6454d','e8b53abd-7c73-483b-bfab-5ff67688eadb',
    '63273d2b-e910-4dca-9673-31d637f0e1c2']::uuid[];
  doc_slugs   text[] := ARRAY['passport','police-clearance','medical-report','saqa-qualifications','bank-statements'];

  first_names text[] := ARRAY['Amara','Bheki','Chloe','Dumisani','Elena','Farai','Gugu','Hassan','Imani','Jabu',
                              'Karabo','Liam','Maya','Nkosana','Olga','Pravin','Qiana','Rudo','Sanele','Tariq'];
  last_names  text[] := ARRAY['Abrahams','Botha','Cele','Dlamini','Eriksen','Fernandes','Govender','Hlongwane','Ivanov','Jacobs',
                              'Khoza','Lombard','Moyo','Ndlovu','Osei','Pillay','Quinn','Radebe','Smit','Tshabalala'];
  cons_names  text[] := ARRAY['Nomsa Dube','Pieter van Wyk','Fatima Essop'];

  reject_notes text[] := ARRAY[
    'The scan is blurry. Please upload a clearer copy.',
    'Document is older than six months. Please upload a recent one.',
    'Some pages are missing. Please upload every page.',
    'The name does not match your passport. Please check and upload again.'];
  cons_msgs   text[] := ARRAY[
    'Welcome aboard. Please upload the documents listed on your case page.',
    'We have reviewed your documents and updated your case.',
    'Your case has moved to the next stage. We will keep you posted.'];
  client_msgs text[] := ARRAY[
    'Hi, I have a question about which documents you need.',
    'Thank you, I have uploaded the new copy.',
    'Is there anything else I should prepare?'];
  lead_reasons text[] := ARRAY[
    'Clear intent, valid status and a realistic start date.',
    'Does not meet the minimum requirements for any visa category we handle.',
    'Details are unclear. A consultant should confirm eligibility.'];

  i int; d int; m int; s int;
  cid uuid; client_id uuid; cons_id uuid; visa_id uuid;
  stg int; created timestamptz; arch boolean;
  st int; dtype int; doc_id uuid;
  trg int; lead_id uuid; submitted timestamptz; reviewer uuid; nm text;
BEGIN
  ---------------------------------------------------------------- users
  FOR i IN 1..3 LOOP
    INSERT INTO "User" ("Id","FullName","Email","PasswordHash","Role","IsActive","EmailConfirmed","CreatedAt")
    VALUES (md5('s3-cons-'||i)::uuid, cons_names[i], 'mock.consultant'||i||'@example.com', pw_cons, 1, true, true,
            now() - make_interval(days => 40 + i * 5))
    ON CONFLICT ("Id") DO NOTHING;
  END LOOP;

  FOR i IN 1..15 LOOP
    INSERT INTO "User" ("Id","FullName","Email","PasswordHash","Role","IsActive","EmailConfirmed","CreatedAt")
    VALUES (md5('s3-client-'||i)::uuid,
            first_names[i] || ' ' || last_names[1 + ((i * 7) % 20)],
            'mock.client' || lpad(i::text, 2, '0') || '@example.com', pw_client, 2, true, true,
            now() - make_interval(days => 3 + i * 4))
    ON CONFLICT ("Id") DO NOTHING;
  END LOOP;

  -- Suspended, soft-deleted and locked-out clients (no cases, so nothing blocks them).
  INSERT INTO "User" ("Id","FullName","Email","PasswordHash","Role","IsActive","EmailConfirmed","CreatedAt","DeletedAt","FailedLoginCount","FailedLoginWindowStart","LockoutEndsAt") VALUES
  (md5('s3-client-susp')::uuid,   'Suspended Sam',  'mock.suspended@example.com', pw_client, 2, false, true, now() - interval '35 days', NULL, 0, NULL, NULL),
  (md5('s3-client-del')::uuid,    'Deleted Dana',   'mock.deleted@example.com',   pw_client, 2, false, true, now() - interval '80 days', now() - interval '9 days', 0, NULL, NULL),
  (md5('s3-client-locked')::uuid, 'Locked Lerato',  'mock.locked@example.com',    pw_client, 2, true,  true, now() - interval '20 days', NULL, 5, now() - interval '2 minutes', now() + interval '15 minutes')
  ON CONFLICT ("Id") DO NOTHING;

  INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt") VALUES
  (md5('s3-audit-susp')::uuid, admin_id, 'Suspended user', 'User', md5('s3-client-susp')::uuid, now() - interval '12 days'),
  (md5('s3-audit-del')::uuid,  admin_id, 'Deleted user',   'User', md5('s3-client-del')::uuid,  now() - interval '9 days')
  ON CONFLICT ("Id") DO NOTHING;

  ---------------------------------------------------------------- cases and everything under them
  FOR i IN 1..30 LOOP
    cid       := md5('s3-case-' || i)::uuid;
    client_id := md5('s3-client-' || (((i - 1) % 15) + 1))::uuid;
    cons_id   := CASE WHEN i % 8 = 0 THEN NULL ELSE consultants[1 + (i % 5)] END;   -- every 8th case is unassigned
    visa_id   := visa_ids[1 + (i % 5)];
    stg       := CASE WHEN cons_id IS NULL THEN 0 ELSE ((i * 3) + (i / 5)) % 5 END;       -- spreads over all 5 stages
    arch      := (i % 9 = 0);
    created   := now() - make_interval(days => 8 + i * 3);

    INSERT INTO "Case" ("Id","LeadId","ClientId","ConsultantId","VisaCategoryId","Stage","IsArchived","CreatedAt","UpdatedAt")
    VALUES (cid, NULL, client_id, cons_id, visa_id, stg, arch, created, now() - make_interval(days => i % 6))
    ON CONFLICT ("Id") DO NOTHING;

    INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt")
    VALUES (md5('s3-audit-case-' || i)::uuid, COALESCE(cons_id, admin_id), 'Created case', 'Case', cid, created)
    ON CONFLICT ("Id") DO NOTHING;

    IF arch THEN
      INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt")
      VALUES (md5('s3-audit-arch-' || i)::uuid, COALESCE(cons_id, admin_id), 'Archived case', 'Case', cid, now() - make_interval(days => i % 6))
      ON CONFLICT ("Id") DO NOTHING;
    END IF;

    IF cons_id IS NULL THEN
      NULL;  -- unassigned: no history, documents or messages yet
    ELSE
      INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt")
      VALUES (md5('s3-audit-assign-' || i)::uuid, admin_id, 'Assigned consultant', 'Case', cid, created + interval '2 hours')
      ON CONFLICT ("Id") DO NOTHING;
    END IF;

    -- stage history, one row per step up to the current stage
    FOR s IN 1..stg LOOP
      INSERT INTO "CaseStageHistory" ("Id","CaseId","ChangedById","FromStage","ToStage","ChangedAt")
      VALUES (md5('s3-hist-' || i || '-' || s)::uuid, cid, cons_id, s - 1, s, created + make_interval(hours => s * 20))
      ON CONFLICT ("Id") DO NOTHING;
      INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt")
      VALUES (md5('s3-audit-hist-' || i || '-' || s)::uuid, cons_id, 'Advanced case stage', 'Case', cid, created + make_interval(hours => s * 20))
      ON CONFLICT ("Id") DO NOTHING;
    END LOOP;

    -- three documents on every case past intake: a mix of pending, approved, rejected, some archived
    IF stg >= 1 THEN
      FOR d IN 1..3 LOOP
        st       := (i + d) % 3;
        dtype    := (i + d) % 5;
        doc_id   := md5('s3-doc-' || i || '-' || d)::uuid;
        INSERT INTO "Document" ("Id","CaseId","DocumentTypeId","ReviewedById","FileName","FilePath","Status","ReviewComment","ReviewedAt","UploadedAt","IsArchived")
        VALUES (doc_id, cid, doc_ids[1 + dtype],
                CASE WHEN st <> 0 THEN cons_id END,
                doc_slugs[1 + dtype] || '-case' || lpad(i::text, 2, '0') || '.pdf',
                'local-test/' || doc_slugs[1 + dtype] || '-case' || lpad(i::text, 2, '0') || '.pdf',
                st,
                CASE WHEN st = 2 THEN reject_notes[1 + ((i + d) % 4)] END,
                CASE WHEN st <> 0 THEN now() - make_interval(days => (i + d) % 7) END,
                now() - make_interval(days => 1 + (i + d) % 14, hours => d),
                (st = 2 AND d = 1 AND i % 4 = 0))
        ON CONFLICT ("Id") DO NOTHING;
        IF st <> 0 THEN
          INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt")
          VALUES (md5('s3-audit-doc-' || i || '-' || d)::uuid, cons_id,
                  CASE WHEN st = 1 THEN 'Approved document' ELSE 'Rejected document' END,
                  'Document', doc_id, now() - make_interval(days => (i + d) % 7))
          ON CONFLICT ("Id") DO NOTHING;
        END IF;
      END LOOP;
    END IF;

    -- a short message thread on every assigned case (older messages read, newest unread)
    IF cons_id IS NOT NULL THEN
      FOR m IN 1..3 LOOP
        INSERT INTO "Message" ("Id","CaseId","SenderId","Body","IsRead","SentAt")
        VALUES (md5('s3-msg-' || i || '-' || m)::uuid, cid,
                CASE WHEN m = 2 THEN client_id ELSE cons_id END,
                CASE WHEN m = 2 THEN client_msgs[1 + (i % 3)] ELSE cons_msgs[m] END,
                (m < 3),
                now() - make_interval(hours => 120 - m * 30 + (i % 5)))
        ON CONFLICT ("Id") DO NOTHING;
      END LOOP;
    END IF;

    -- finished cases get a visa record, some flagged for renewal
    IF stg = 4 THEN
      INSERT INTO "VisaRecord" ("Id","CaseId","VisaType","IssuedDate","ExpiryDate","RenewalFlagged")
      VALUES (md5('s3-visarec-' || i)::uuid, cid, visa_names[1 + (i % 5)],
              now() - interval '60 days', now() + make_interval(days => 30 + i * 20), (i % 2 = 0))
      ON CONFLICT ("Id") DO NOTHING;
    END IF;
  END LOOP;

  ---------------------------------------------------------------- leads (every triage state)
  FOR i IN 1..40 LOOP
    lead_id   := md5('s3-lead-' || i)::uuid;
    trg       := i % 4;
    submitted := now() - make_interval(hours => i * 17);
    reviewer  := CASE WHEN trg IN (1, 2) AND i % 2 = 0 THEN consultants[1 + (i % 5)] END;
    nm        := first_names[1 + (i % 20)] || ' ' || last_names[1 + ((i * 7) % 20)];

    INSERT INTO "Lead" ("Id","ReviewedById","VisaCategoryId","FullName","Email","ImmigrationStatus","NeedsAssistance",
                        "DesiredStartDate","TriageStatus","TriageReason","SubmittedAt","ConsentAt")
    VALUES (lead_id, reviewer,
            CASE WHEN i % 3 = 0 THEN NULL ELSE visa_ids[1 + (i % 5)] END,
            nm,
            lower(first_names[1 + (i % 20)] || '.' || last_names[1 + ((i * 7) % 20)]) || i || '@example.com',
            i % 4, (i % 3 <> 0),
            now() + make_interval(days => 15 + i * 3),
            trg,
            CASE WHEN trg = 0 THEN NULL ELSE lead_reasons[LEAST(trg, 3)] END,
            submitted,
            CASE WHEN i % 13 = 0 THEN NULL ELSE submitted END)   -- a few legacy leads with no recorded consent
    ON CONFLICT ("Id") DO NOTHING;

    IF trg <> 0 THEN
      INSERT INTO "AgentRun" ("Id","CaseId","LeadId","AgentType","Output","Status","RanAt")
      VALUES (md5('s3-run-' || i)::uuid, NULL, lead_id, 0,
              CASE WHEN trg = 3
                   THEN '{"message":"AI unavailable. Routed to consultant review."}'
                   ELSE '{"status":"' || CASE WHEN trg = 1 THEN 'Qualified' ELSE 'Unqualified' END
                        || '","reason":"' || lead_reasons[trg] || '"}' END,
              CASE WHEN trg = 3 THEN (i % 2) + 1 ELSE 0 END,      -- NeedsReview: alternate Fallback and Error
              submitted + interval '2 seconds')
      ON CONFLICT ("Id") DO NOTHING;
    END IF;

    IF reviewer IS NOT NULL THEN
      INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt")
      VALUES (md5('s3-audit-lead-' || i)::uuid, reviewer, 'Reviewed lead', 'Lead', lead_id, submitted + interval '3 hours')
      ON CONFLICT ("Id") DO NOTHING;
    END IF;
  END LOOP;
END $$;

COMMIT;
