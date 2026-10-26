-- DEMO DATA ONLY. Run after 1-reference-and-users.sql. See database/seed/README.md.
-- ImmigrationStatus: Citizen=0 PermanentResident=1 TemporaryResident=2 Foreigner=3
-- TriageStatus: Pending=0 Qualified=1 Unqualified=2 NeedsReview=3
-- CaseStage: InitialIntake=0 DocumentGathering=1 UnderReview=2 SubmittedToAuthority=3 FinalDecision=4
-- DocumentStatus: Pending=0 Approved=1 Rejected=2
BEGIN;

INSERT INTO "Lead" ("Id","ReviewedById","VisaCategoryId","FullName","Email","ImmigrationStatus","NeedsAssistance","DesiredStartDate","TriageStatus","SubmittedAt","ConsentGivenAt") VALUES
('10ffbfb4-3b55-47e1-b630-dee8bac827ad',NULL,'4822fd67-4407-448f-aed6-bb6997f2e849','Naledi Khumalo','naledi.khumalo@example.com',3,true,now()+interval '60 days',3,now()-interval '2 hours',now()-interval '2 hours'),
('01e4741e-dd38-4810-9be2-5148865be47a',NULL,'2c0239e6-8648-4ba9-9f9b-e8e614fd2e05','Omar Haddad','omar.haddad@example.com',2,false,now()+interval '45 days',1,now()-interval '1 day',now()-interval '1 day'),
('8423fbeb-64f8-43c8-8732-8430ce348d51',NULL,NULL,'Sarah Jensen','sarah.jensen@example.com',3,true,now()+interval '90 days',3,now()-interval '29 hours',now()-interval '29 hours'),
('ba1926ff-7e03-4343-aa05-7aeee313bf42',NULL,NULL,'Bongani Zulu','bongani.zulu@example.com',0,false,now()+interval '30 days',2,now()-interval '2 days',now()-interval '2 days'),
('d2fb31bb-0ce8-4196-8dae-b6125d6b3572','33333333-3333-3333-3333-333333333334','35f3c178-f925-4725-941f-ef0cf10761ae','Priya Naidoo','priya.naidoo@example.com',1,true,now()+interval '120 days',1,now()-interval '3 days',now()-interval '3 days'),
('a15ed80c-83a5-4c22-a9dd-861bb02f8ffa',NULL,NULL,'Tomas Silva','tomas.silva@example.com',3,true,now()+interval '75 days',0,now()-interval '4 days',now()-interval '4 days'),
('ac5e6184-1361-4a80-9fd8-a37f1609a85b',NULL,'29542358-d41e-4e4f-9340-2a0d0bb22ae6','Kwame Asante','kwame.asante@example.com',2,true,now()+interval '50 days',3,now()-interval '5 days',now()-interval '5 days'),
('8bd4dc25-c065-4136-8232-db1e5520c399',NULL,'4822fd67-4407-448f-aed6-bb6997f2e849','Emily Clarke','emily.clarke@example.com',3,false,now()+interval '100 days',1,now()-interval '6 days',now()-interval '6 days'),
('f2a5f907-4e9f-411a-b950-eabaca644c74',NULL,NULL,'Lindiwe Mthembu','lindiwe.mthembu@example.com',0,true,now()+interval '20 days',2,now()-interval '8 days',now()-interval '8 days'),
('9acc97d0-70ef-4b87-b72c-b4df4f304fe6','33333333-3333-3333-3333-333333333334','2c0239e6-8648-4ba9-9f9b-e8e614fd2e05','Ahmed Yusuf','ahmed.yusuf@example.com',3,true,now()+interval '65 days',1,now()-interval '10 days',now()-interval '10 days'),
('eedbc1f8-5908-43da-88ea-fd435b9b87fe','33333333-3333-3333-3333-333333333334','4822fd67-4407-448f-aed6-bb6997f2e849','John Doe','client@nexus.com',3,true,now()+interval '30 days',1,now()-interval '30 days',now()-interval '30 days');

INSERT INTO "Case" ("Id","LeadId","ClientId","ConsultantId","VisaCategoryId","Stage","IsArchived","CreatedAt","UpdatedAt") VALUES
('6adcbe68-5bbb-48cf-87b8-aeb22118714e','eedbc1f8-5908-43da-88ea-fd435b9b87fe','77777777-7777-7777-7777-777777777777','33333333-3333-3333-3333-333333333334','4822fd67-4407-448f-aed6-bb6997f2e849',2,false,now()-interval '28 days',now()-interval '1 day'),
('2bb90b10-5b20-4bc0-9614-c11436bb5b7e',NULL,'b8cd5c18-2134-4a00-9780-dfeb7139c474','33333333-3333-3333-3333-333333333334','2c0239e6-8648-4ba9-9f9b-e8e614fd2e05',0,false,now()-interval '2 days',now()-interval '2 days'),
('89706d58-9dd8-44f9-a042-4416c18b4cd8',NULL,'603cd4bd-8435-4250-8d96-42eebd76af71','33333333-3333-3333-3333-333333333334','29542358-d41e-4e4f-9340-2a0d0bb22ae6',1,false,now()-interval '12 days',now()-interval '3 days'),
('8f415e8c-c772-41a2-b1f9-c71721dd7f7f',NULL,'1a89cabd-f630-46a3-ae42-0219e47c7ca3','33333333-3333-3333-3333-333333333334','35b7aa88-ff7f-4a3c-9489-8f176c705c76',3,false,now()-interval '40 days',now()-interval '6 days'),
('9fc4401b-a7a1-44e1-ae11-f9da663462c5',NULL,'ed41d723-186c-471b-93e9-ec62e51cc211','33333333-3333-3333-3333-333333333333','35f3c178-f925-4725-941f-ef0cf10761ae',4,false,now()-interval '90 days',now()-interval '10 days'),
('c9db96f5-e49c-4897-bf49-5e647bc47b5c',NULL,'37a7d98e-94f6-4b66-aa3a-155bbffc3144','33333333-3333-3333-3333-333333333334','2c0239e6-8648-4ba9-9f9b-e8e614fd2e05',1,true,now()-interval '70 days',now()-interval '50 days'),
('03bab703-8306-49ae-8188-f03ecd16bc24',NULL,'09be0a24-d50b-469a-bcd2-9668c93f3d55',NULL,'4822fd67-4407-448f-aed6-bb6997f2e849',0,false,now()-interval '1 day',now()-interval '1 day');

INSERT INTO "CaseStageHistory" ("Id","CaseId","ChangedById","FromStage","ToStage","ChangedAt") VALUES
(gen_random_uuid(),'6adcbe68-5bbb-48cf-87b8-aeb22118714e','33333333-3333-3333-3333-333333333334',0,1,now()-interval '20 days'),
(gen_random_uuid(),'6adcbe68-5bbb-48cf-87b8-aeb22118714e','33333333-3333-3333-3333-333333333334',1,2,now()-interval '1 day'),
(gen_random_uuid(),'8f415e8c-c772-41a2-b1f9-c71721dd7f7f','33333333-3333-3333-3333-333333333334',0,1,now()-interval '35 days'),
(gen_random_uuid(),'8f415e8c-c772-41a2-b1f9-c71721dd7f7f','33333333-3333-3333-3333-333333333334',1,2,now()-interval '20 days'),
(gen_random_uuid(),'8f415e8c-c772-41a2-b1f9-c71721dd7f7f','33333333-3333-3333-3333-333333333334',2,3,now()-interval '6 days'),
(gen_random_uuid(),'9fc4401b-a7a1-44e1-ae11-f9da663462c5','33333333-3333-3333-3333-333333333333',3,4,now()-interval '10 days');

INSERT INTO "Document" ("Id","CaseId","DocumentTypeId","ReviewedById","FileName","FilePath","Status","ReviewComment","UploadedAt","IsArchived") VALUES
('a59f0c67-bfbd-4a9b-9245-7e93bc3acf5a','6adcbe68-5bbb-48cf-87b8-aeb22118714e','d29caa63-37df-4fe6-98ca-b1cc49550469','33333333-3333-3333-3333-333333333334','passport-john-doe.pdf','local-test/passport-john-doe.pdf',1,NULL,now()-interval '25 days',false),
('293448b1-b8d6-430a-82a4-696783265f9d','6adcbe68-5bbb-48cf-87b8-aeb22118714e','c6860be5-e0d7-452a-a248-56796622a648',NULL,'police-clearance-john-doe.pdf','local-test/police-clearance-john-doe.pdf',0,NULL,now()-interval '3 hours',false),
('72f7316f-51a3-4688-be1b-06769fa77ae7','6adcbe68-5bbb-48cf-87b8-aeb22118714e','f6a454ac-d6be-47b8-bd54-626654e6454d','33333333-3333-3333-3333-333333333334','medical-report-john-doe.pdf','local-test/medical-report-john-doe.pdf',2,'Report is older than six months. Please upload a recent one.',now()-interval '10 days',false),
('774a8bb0-d5e9-4124-9150-4011290194b9','89706d58-9dd8-44f9-a042-4416c18b4cd8','d29caa63-37df-4fe6-98ca-b1cc49550469',NULL,'passport-ayesha-patel.pdf','local-test/passport-ayesha-patel.pdf',0,NULL,now()-interval '3 days',false),
('bbb240db-428b-4b5b-9bc1-04f1affce095','8f415e8c-c772-41a2-b1f9-c71721dd7f7f','63273d2b-e910-4dca-9673-31d637f0e1c2','33333333-3333-3333-3333-333333333334','bank-statements-lucas-ferreira.pdf','local-test/bank-statements-lucas-ferreira.pdf',1,NULL,now()-interval '25 days',false);

INSERT INTO "Message" ("Id","CaseId","SenderId","Body","IsRead","SentAt") VALUES
(gen_random_uuid(),'6adcbe68-5bbb-48cf-87b8-aeb22118714e','33333333-3333-3333-3333-333333333334','Your passport scan has been approved.',false,now()-interval '1 day'),
(gen_random_uuid(),'6adcbe68-5bbb-48cf-87b8-aeb22118714e','33333333-3333-3333-3333-333333333334','Please upload a medical report dated within the last six months.',false,now()-interval '23 hours'),
(gen_random_uuid(),'6adcbe68-5bbb-48cf-87b8-aeb22118714e','77777777-7777-7777-7777-777777777777','Thanks, I have uploaded my police clearance certificate.',false,now()-interval '3 hours'),
(gen_random_uuid(),'89706d58-9dd8-44f9-a042-4416c18b4cd8','603cd4bd-8435-4250-8d96-42eebd76af71','Which bank statements do you need, personal or business?',false,now()-interval '5 hours'),
(gen_random_uuid(),'8f415e8c-c772-41a2-b1f9-c71721dd7f7f','33333333-3333-3333-3333-333333333334','Your application has been submitted to the Department of Home Affairs.',true,now()-interval '6 days'),
(gen_random_uuid(),'9fc4401b-a7a1-44e1-ae11-f9da663462c5','33333333-3333-3333-3333-333333333333','A final decision has been issued on your application.',true,now()-interval '10 days');

INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt") VALUES
(gen_random_uuid(),'00000000-0000-0000-0000-000000000001','Created user','User','77777777-7777-7777-7777-777777777777',now()-interval '30 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Created case','Case','6adcbe68-5bbb-48cf-87b8-aeb22118714e',now()-interval '28 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Archived case','Case','c9db96f5-e49c-4897-bf49-5e647bc47b5c',now()-interval '50 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333333','Advanced case stage','Case','9fc4401b-a7a1-44e1-ae11-f9da663462c5',now()-interval '10 days'),
(gen_random_uuid(),'00000000-0000-0000-0000-000000000001','Suspended user','User','3851c5ce-bbe3-4d68-97c3-4c69d1d469c0',now()-interval '7 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Reviewed lead','Lead','d2fb31bb-0ce8-4196-8dae-b6125d6b3572',now()-interval '3 days'),
(gen_random_uuid(),'00000000-0000-0000-0000-000000000001','Assigned consultant','Case','2bb90b10-5b20-4bc0-9614-c11436bb5b7e',now()-interval '2 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Advanced case stage','Case','6adcbe68-5bbb-48cf-87b8-aeb22118714e',now()-interval '1 day'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Approved document','Document','a59f0c67-bfbd-4a9b-9245-7e93bc3acf5a',now()-interval '1 day'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Rejected document','Document','72f7316f-51a3-4688-be1b-06769fa77ae7',now()-interval '23 hours');
COMMIT;
