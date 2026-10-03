-- DEMO DATA ONLY: never load into a database holding real client data. Safe to re-run: wipes and reloads every application table. See database/seed/README.md.
-- Enums are stored as integers. UserRole: Admin=0 Consultant=1 Client=2.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;

TRUNCATE "AuditLog","CaseStageHistory","Message","Document","AgentRun","VisaRecord","ChecklistTemplate","Case","Lead","DocumentType","VisaCategory","User" CASCADE;

-- Same ids and passwords as the API's built-in dev accounts, so sign-in behaves exactly as before.
-- bcrypt cost 11 ($2a$11$...), matching the API's BCrypt work factor.
INSERT INTO "User" ("Id","FullName","Email","PasswordHash","Role","IsActive","EmailConfirmed","CreatedAt") VALUES
('00000000-0000-0000-0000-000000000001','System Admin','admin@nexus.com',crypt('AdminPassword123!',gen_salt('bf',11)),0,true,true,now()-interval '60 days'),
('33333333-3333-3333-3333-333333333334','Consultant User','consultant@nexus.com',crypt('ConsultantPassword123!',gen_salt('bf',11)),1,true,true,now()-interval '55 days'),
('33333333-3333-3333-3333-333333333333','Riaad','riaad@nexus.com',crypt('ConsultantPassword123!',gen_salt('bf',11)),1,true,true,now()-interval '50 days'),
('33333333-3333-3333-3333-333333333335','Sipho Dlamini','sipho.dlamini@nexus.com',crypt('ConsultantPassword123!',gen_salt('bf',11)),1,false,true,now()-interval '45 days'),
('77777777-7777-7777-7777-777777777777','John Doe','client@nexus.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '30 days'),
('77777777-7777-7777-7777-777777777701','Thandiwe Mokoena','thandiwe.mokoena@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '3 days'),
('77777777-7777-7777-7777-777777777702','Ayesha Patel','ayesha.patel@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '13 days'),
('77777777-7777-7777-7777-777777777703','Lucas Ferreira','lucas.ferreira@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '41 days'),
('77777777-7777-7777-7777-777777777704','Grace Nkosi','grace.nkosi@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '91 days'),
('77777777-7777-7777-7777-777777777705','Mei Chen','mei.chen@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '71 days'),
('77777777-7777-7777-7777-777777777706','Daniel Okafor','daniel.okafor@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '1 day');

-- IsActive written explicitly: a bool with a database default would ignore false (see note to Lekabe).
INSERT INTO "VisaCategory" ("Id","Name","Description","IsActive") VALUES
('aaaaaaaa-0000-0000-0000-000000000001','Critical Skills Work Visa','For applicants with skills on the Critical Skills List.',true),
('aaaaaaaa-0000-0000-0000-000000000002','General Work Visa','Employment with a South African employer.',true),
('aaaaaaaa-0000-0000-0000-000000000003','Spousal Visa','Spouse or life partner of a South African citizen or resident.',true),
('aaaaaaaa-0000-0000-0000-000000000004','Study Visa','Full-time study at a registered institution.',true),
('aaaaaaaa-0000-0000-0000-000000000005','Permanent Residence','Section 26 and 27 permanent residence permits.',true);

INSERT INTO "DocumentType" ("Id","Name","Description") VALUES
('bbbbbbbb-0000-0000-0000-000000000001','Passport','Certified copy of all used pages.'),
('bbbbbbbb-0000-0000-0000-000000000002','Police clearance certificate','From every country lived in for 12 months or more.'),
('bbbbbbbb-0000-0000-0000-000000000003','Medical report','Issued within the last six months.'),
('bbbbbbbb-0000-0000-0000-000000000004','Proof of qualifications (SAQA)','SAQA evaluation of foreign qualifications.'),
('bbbbbbbb-0000-0000-0000-000000000005','Bank statements (3 months)','Most recent three months, stamped by the bank.');
COMMIT;
