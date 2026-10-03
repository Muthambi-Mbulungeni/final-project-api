-- LOCAL TEST DATA ONLY. Run after 1-reference-and-users.sql.
-- ImmigrationStatus: Citizen=0 PermanentResident=1 TemporaryResident=2 Foreigner=3
-- TriageStatus: Pending=0 Qualified=1 Unqualified=2 NeedsReview=3
-- CaseStage: InitialIntake=0 DocumentGathering=1 UnderReview=2 SubmittedToAuthority=3 FinalDecision=4
-- DocumentStatus: Pending=0 Approved=1 Rejected=2
BEGIN;

INSERT INTO "Lead" ("Id","ReviewedById","VisaCategoryId","FullName","Email","ImmigrationStatus","NeedsAssistance","DesiredStartDate","TriageStatus","SubmittedAt") VALUES
('cccccccc-0000-0000-0000-000000000001',NULL,'aaaaaaaa-0000-0000-0000-000000000001','Naledi Khumalo','naledi.khumalo@example.com',3,true,now()+interval '60 days',3,now()-interval '2 hours'),
('cccccccc-0000-0000-0000-000000000002',NULL,'aaaaaaaa-0000-0000-0000-000000000002','Omar Haddad','omar.haddad@example.com',2,false,now()+interval '45 days',1,now()-interval '1 day'),
('cccccccc-0000-0000-0000-000000000003',NULL,NULL,'Sarah Jensen','sarah.jensen@example.com',3,true,now()+interval '90 days',3,now()-interval '29 hours'),
('cccccccc-0000-0000-0000-000000000004',NULL,NULL,'Bongani Zulu','bongani.zulu@example.com',0,false,now()+interval '30 days',2,now()-interval '2 days'),
('cccccccc-0000-0000-0000-000000000005','33333333-3333-3333-3333-333333333334','aaaaaaaa-0000-0000-0000-000000000005','Priya Naidoo','priya.naidoo@example.com',1,true,now()+interval '120 days',1,now()-interval '3 days'),
('cccccccc-0000-0000-0000-000000000006',NULL,NULL,'Tomas Silva','tomas.silva@example.com',3,true,now()+interval '75 days',0,now()-interval '4 days'),
('cccccccc-0000-0000-0000-000000000007',NULL,'aaaaaaaa-0000-0000-0000-000000000004','Kwame Asante','kwame.asante@example.com',2,true,now()+interval '50 days',3,now()-interval '5 days'),
('cccccccc-0000-0000-0000-000000000008',NULL,'aaaaaaaa-0000-0000-0000-000000000001','Emily Clarke','emily.clarke@example.com',3,false,now()+interval '100 days',1,now()-interval '6 days'),
('cccccccc-0000-0000-0000-000000000009',NULL,NULL,'Lindiwe Mthembu','lindiwe.mthembu@example.com',0,true,now()+interval '20 days',2,now()-interval '8 days'),
('cccccccc-0000-0000-0000-000000000010','33333333-3333-3333-3333-333333333334','aaaaaaaa-0000-0000-0000-000000000002','Ahmed Yusuf','ahmed.yusuf@example.com',3,true,now()+interval '65 days',1,now()-interval '10 days'),
('cccccccc-0000-0000-0000-000000000011','33333333-3333-3333-3333-333333333334','aaaaaaaa-0000-0000-0000-000000000001','John Doe','client@nexus.com',3,true,now()+interval '30 days',1,now()-interval '30 days');

INSERT INTO "Case" ("Id","LeadId","ClientId","ConsultantId","VisaCategoryId","Stage","IsArchived","CreatedAt","UpdatedAt") VALUES
('dddddddd-0000-0000-0000-000000000001','cccccccc-0000-0000-0000-000000000011','77777777-7777-7777-7777-777777777777','33333333-3333-3333-3333-333333333334','aaaaaaaa-0000-0000-0000-000000000001',2,false,now()-interval '28 days',now()-interval '1 day'),
('dddddddd-0000-0000-0000-000000000002',NULL,'77777777-7777-7777-7777-777777777701','33333333-3333-3333-3333-333333333334','aaaaaaaa-0000-0000-0000-000000000002',0,false,now()-interval '2 days',now()-interval '2 days'),
('dddddddd-0000-0000-0000-000000000003',NULL,'77777777-7777-7777-7777-777777777702','33333333-3333-3333-3333-333333333334','aaaaaaaa-0000-0000-0000-000000000004',1,false,now()-interval '12 days',now()-interval '3 days'),
('dddddddd-0000-0000-0000-000000000004',NULL,'77777777-7777-7777-7777-777777777703','33333333-3333-3333-3333-333333333334','aaaaaaaa-0000-0000-0000-000000000003',3,false,now()-interval '40 days',now()-interval '6 days'),
('dddddddd-0000-0000-0000-000000000005',NULL,'77777777-7777-7777-7777-777777777704','33333333-3333-3333-3333-333333333333','aaaaaaaa-0000-0000-0000-000000000005',4,false,now()-interval '90 days',now()-interval '10 days'),
('dddddddd-0000-0000-0000-000000000006',NULL,'77777777-7777-7777-7777-777777777705','33333333-3333-3333-3333-333333333334','aaaaaaaa-0000-0000-0000-000000000002',1,true,now()-interval '70 days',now()-interval '50 days'),
('dddddddd-0000-0000-0000-000000000007',NULL,'77777777-7777-7777-7777-777777777706',NULL,'aaaaaaaa-0000-0000-0000-000000000001',0,false,now()-interval '1 day',now()-interval '1 day');

INSERT INTO "CaseStageHistory" ("Id","CaseId","ChangedById","FromStage","ToStage","ChangedAt") VALUES
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000001','33333333-3333-3333-3333-333333333334',0,1,now()-interval '20 days'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000001','33333333-3333-3333-3333-333333333334',1,2,now()-interval '1 day'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000004','33333333-3333-3333-3333-333333333334',0,1,now()-interval '35 days'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000004','33333333-3333-3333-3333-333333333334',1,2,now()-interval '20 days'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000004','33333333-3333-3333-3333-333333333334',2,3,now()-interval '6 days'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000005','33333333-3333-3333-3333-333333333333',3,4,now()-interval '10 days');

INSERT INTO "Document" ("Id","CaseId","DocumentTypeId","ReviewedById","FileName","FilePath","Status","ReviewComment","UploadedAt","IsArchived") VALUES
('eeeeeeee-0000-0000-0000-000000000001','dddddddd-0000-0000-0000-000000000001','bbbbbbbb-0000-0000-0000-000000000001','33333333-3333-3333-3333-333333333334','passport-john-doe.pdf','local-test/passport-john-doe.pdf',1,NULL,now()-interval '25 days',false),
('eeeeeeee-0000-0000-0000-000000000002','dddddddd-0000-0000-0000-000000000001','bbbbbbbb-0000-0000-0000-000000000002',NULL,'police-clearance-john-doe.pdf','local-test/police-clearance-john-doe.pdf',0,NULL,now()-interval '3 hours',false),
('eeeeeeee-0000-0000-0000-000000000003','dddddddd-0000-0000-0000-000000000001','bbbbbbbb-0000-0000-0000-000000000003','33333333-3333-3333-3333-333333333334','medical-report-john-doe.pdf','local-test/medical-report-john-doe.pdf',2,'Report is older than six months. Please upload a recent one.',now()-interval '10 days',false),
('eeeeeeee-0000-0000-0000-000000000004','dddddddd-0000-0000-0000-000000000003','bbbbbbbb-0000-0000-0000-000000000001',NULL,'passport-ayesha-patel.pdf','local-test/passport-ayesha-patel.pdf',0,NULL,now()-interval '3 days',false),
('eeeeeeee-0000-0000-0000-000000000005','dddddddd-0000-0000-0000-000000000004','bbbbbbbb-0000-0000-0000-000000000005','33333333-3333-3333-3333-333333333334','bank-statements-lucas-ferreira.pdf','local-test/bank-statements-lucas-ferreira.pdf',1,NULL,now()-interval '25 days',false);

INSERT INTO "Message" ("Id","CaseId","SenderId","Body","IsRead","SentAt") VALUES
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000001','33333333-3333-3333-3333-333333333334','Your passport scan has been approved.',false,now()-interval '1 day'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000001','33333333-3333-3333-3333-333333333334','Please upload a medical report dated within the last six months.',false,now()-interval '23 hours'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000001','77777777-7777-7777-7777-777777777777','Thanks, I have uploaded my police clearance certificate.',false,now()-interval '3 hours'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000003','77777777-7777-7777-7777-777777777702','Which bank statements do you need, personal or business?',false,now()-interval '5 hours'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000004','33333333-3333-3333-3333-333333333334','Your application has been submitted to the Department of Home Affairs.',true,now()-interval '6 days'),
(gen_random_uuid(),'dddddddd-0000-0000-0000-000000000005','33333333-3333-3333-3333-333333333333','A final decision has been issued on your application.',true,now()-interval '10 days');

INSERT INTO "AuditLog" ("Id","UserId","Action","EntityType","EntityId","CreatedAt") VALUES
(gen_random_uuid(),'00000000-0000-0000-0000-000000000001','Created user','User','77777777-7777-7777-7777-777777777777',now()-interval '30 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Created case','Case','dddddddd-0000-0000-0000-000000000001',now()-interval '28 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Archived case','Case','dddddddd-0000-0000-0000-000000000006',now()-interval '50 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333333','Advanced case stage','Case','dddddddd-0000-0000-0000-000000000005',now()-interval '10 days'),
(gen_random_uuid(),'00000000-0000-0000-0000-000000000001','Suspended user','User','33333333-3333-3333-3333-333333333335',now()-interval '7 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Reviewed lead','Lead','cccccccc-0000-0000-0000-000000000005',now()-interval '3 days'),
(gen_random_uuid(),'00000000-0000-0000-0000-000000000001','Assigned consultant','Case','dddddddd-0000-0000-0000-000000000002',now()-interval '2 days'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Advanced case stage','Case','dddddddd-0000-0000-0000-000000000001',now()-interval '1 day'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Approved document','Document','eeeeeeee-0000-0000-0000-000000000001',now()-interval '1 day'),
(gen_random_uuid(),'33333333-3333-3333-3333-333333333334','Rejected document','Document','eeeeeeee-0000-0000-0000-000000000003',now()-interval '23 hours');
COMMIT;
