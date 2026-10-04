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
('3851c5ce-bbe3-4d68-97c3-4c69d1d469c0','Sipho Dlamini','sipho.dlamini@nexus.com',crypt('ConsultantPassword123!',gen_salt('bf',11)),1,false,true,now()-interval '45 days'),
('77777777-7777-7777-7777-777777777777','John Doe','client@nexus.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '30 days'),
('b8cd5c18-2134-4a00-9780-dfeb7139c474','Thandiwe Mokoena','thandiwe.mokoena@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '3 days'),
('603cd4bd-8435-4250-8d96-42eebd76af71','Ayesha Patel','ayesha.patel@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '13 days'),
('1a89cabd-f630-46a3-ae42-0219e47c7ca3','Lucas Ferreira','lucas.ferreira@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '41 days'),
('ed41d723-186c-471b-93e9-ec62e51cc211','Grace Nkosi','grace.nkosi@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '91 days'),
('37a7d98e-94f6-4b66-aa3a-155bbffc3144','Mei Chen','mei.chen@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '71 days'),
('09be0a24-d50b-469a-bcd2-9668c93f3d55','Daniel Okafor','daniel.okafor@example.com',crypt('ClientPassword123!',gen_salt('bf',11)),2,true,true,now()-interval '1 day');

-- IsActive written explicitly: a bool with a database default would ignore false (see note to Lekabe).
INSERT INTO "VisaCategory" ("Id","Name","Description","IsActive") VALUES
('4822fd67-4407-448f-aed6-bb6997f2e849','Critical Skills Work Visa','For applicants with skills on the Critical Skills List.',true),
('2c0239e6-8648-4ba9-9f9b-e8e614fd2e05','General Work Visa','Employment with a South African employer.',true),
('35b7aa88-ff7f-4a3c-9489-8f176c705c76','Spousal Visa','Spouse or life partner of a South African citizen or resident.',true),
('29542358-d41e-4e4f-9340-2a0d0bb22ae6','Study Visa','Full-time study at a registered institution.',true),
('35f3c178-f925-4725-941f-ef0cf10761ae','Permanent Residence','Section 26 and 27 permanent residence permits.',true);

INSERT INTO "DocumentType" ("Id","Name","Description") VALUES
('d29caa63-37df-4fe6-98ca-b1cc49550469','Passport','Certified copy of all used pages.'),
('c6860be5-e0d7-452a-a248-56796622a648','Police clearance certificate','From every country lived in for 12 months or more.'),
('f6a454ac-d6be-47b8-bd54-626654e6454d','Medical report','Issued within the last six months.'),
('e8b53abd-7c73-483b-bfab-5ff67688eadb','Proof of qualifications (SAQA)','SAQA evaluation of foreign qualifications.'),
('63273d2b-e910-4dca-9673-31d637f0e1c2','Bank statements (3 months)','Most recent three months, stamped by the bank.');
COMMIT;
