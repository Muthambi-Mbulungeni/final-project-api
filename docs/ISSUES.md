# Issue log

Strategies Migration Platform, Team Nexus Solutions (INSY7315).

Known issues at the time of submission, checked against the code on `develop` (`e2751ac`, 2026-10-05, after the final API patches in PR #25). Test IDs (A1, B1 and so on) refer to [TESTING.md](TESTING.md).

**Severity:** Critical = a security risk or data loss. High = a rubric requirement is missing or weakened. Medium = wrong behaviour with a workaround. Low = wording or polish.

**How issues are handled.** Issues are found by the tests in TESTING.md or by reviewing the code. Each one is checked in the code, reported to the owner of that part of the system with a suggested fix, and checked again after the fix is merged. Fixed issues move to section 3.

## 1. Open issues

| ID | Severity | Issue | Owner |
|---|---|---|---|
| [ISS-02](#iss-02) | High | Development secrets committed in `appsettings.json` | Kaan, Mbulungeni |
| [ISS-08](#iss-08) | Medium | Gemini model name may be retired; live call not tested | Marco |
| [ISS-09](#iss-09) | Medium | Uploaded files are stored on the API server's disk | Kaan, Mbulungeni |
| [ISS-10](#iss-10) | Medium | Several API controllers have no automated tests | Marco, Mbulungeni |
| [ISS-11](#iss-11) | Medium | Mobile: 4 manual checks not run | Kundai |
| [ISS-14](#iss-14) | Low | Client logo is low resolution | Riaad |

### ISS-02

**High. Development secrets committed in `appsettings.json`.**

- **Problem:** the development database password and JWT signing key were committed in `Strategies.Api/appsettings.json`.
- **Evidence:** both are now only in `appsettings.Development.json`, which is never loaded outside Development. Outside Development the API refuses to start unless `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `App:BaseUrl` and `Cors:AllowedOrigins` are set, so a hosted deployment cannot fall back to the committed values (`RequiredSettings`, `StartupConfigurationTests`).
- **Impact:** low while they stay development-only. The values remain in the repository and its history, so anyone with access could sign tokens for a host that uses the same key.
- **Fix (remaining):** never reuse these values on a host; set each host's own as application settings. If any hosted environment ever used them, rotate the JWT key and the database password there.


### ISS-08

**Medium. Gemini model name may be retired; live call not tested.**

- **Problem:** the API called `gemini-1.5-flash`, which may no longer be available. The test environment has no Gemini key, so a live call was not tested.
- **Evidence:** `Gemini:Model` in `appsettings.json`; A14 shows the fallback.
- **Impact:** if the call fails, every enquiry is saved as NeedsReview, so no enquiry is lost but none is triaged automatically.
- **Fix:** the code fix is done. The model is now set by `Gemini:Model` (`gemini-3.8-flash-lite`), the key is sent in the `x-goog-api-key` header, the call has a 10-second timeout, instructions are in `systemInstruction` with applicant data treated as untrusted, name and email are not sent, the reply is checked against a JSON schema, and every run is logged to `AgentRun`. Unit tests in `GeminiAgentServiceTests.cs` cover each fallback path. **Remaining:** test one real call with a key.

### ISS-09

**Medium. Uploaded files are stored on the API server's disk.**

- **Problem:** uploads are saved to the API server's disk, not to cloud storage (R2 was planned).
- **Evidence:** `LocalFileStorage`, rooted at `Storage:UploadsRoot` (default `wwwroot/uploads`). A host can point it at a persistent folder outside the deploy folder, such as `/home/data/uploads` on Azure App Service.
- **Impact:** on a host whose disk is reset on each deploy, files would be lost while their database rows remain.
- **Fix:** move uploads to R2 or another object store, or use a host with a persistent disk.

### ISS-10

**Medium. Several API controllers have no automated tests.**

- **Problem:** the API suite (360 tests) covers the services and part of the controllers. Role checks, validation responses and audit rows on the partly covered controllers are checked by hand (TESTING.md section 5).
- **Covered now:** `AuthController` over HTTP in a test server (`AuthControllerTests`: login, refresh and its alias, revoke and its alias, `me`, register, email confirmation, resend confirmation, forgot password), `DocumentTypesController` (`DocumentTypesControllerTests`), `NotificationController` (`NotificationControllerTests`), `StatsController` (`StatsControllerTests`), `MessagesController` (`MessagesControllerTests`), `AdminController` user actions (`AdminUserTests`), `DocumentsController` review, upload and download (`DocumentReviewerTests`, `DocumentsControllerTests`), `CasesController` list and assign (`CaseListTests`, `CaseAssignTests`), `LeadsController` create, consent and rate limit (`LeadConsentTests`, `LeadsRateLimitTests`, `GeminiAgentServiceTests`). The rate limit and startup checks run through the real pipeline in a test server (`WebApplicationFactory`).
- **No tests at all:** none. Every controller now has HTTP-level tests.
- **Partly covered:** `CasesController` create, stage change, export and detail; `LeadsController` list and status update; `AdminController` user list and audit log.
- **Fix:** add tests for the controllers listed above, using `WebApplicationFactory` against a test database, run in CI.

### ISS-11

**Medium. Mobile: 4 manual checks not run.**

- **Problem:** checks 7 (rate limit), 8 (reopen app), 9 (expired access token) and 11 (API stopped) are not yet run. ~~Release builds pointed at `https://CHANGE-ME.example.com/`.~~
- **Fix (partial):** the release build API address is now set to `https://final-project-api-1.onrender.com/` in `strategies-mobile/app/build.gradle.kts` (chore/phase0-fixes). The 4 manual checks remain open — Kundai to run.
- **Evidence:** [mobile test log](../strategies-mobile/TESTING.md); `strategies-mobile/app/build.gradle.kts`.

### ISS-14

**Low. Client logo is low resolution.**

- **Problem:** the logo is a small PNG without transparency and looks soft on high-resolution screens.
- **Fix:** ask the client for an SVG or a 2x transparent PNG.
- **Note:** Riaad will replace the logo asset in the web portal himself; no API change is needed.

## 2. Planned but not delivered

These were in the original plan or came up in the client interview, and are not in this submission. The database already has tables for most of them, so they can be added without changing the design.

| Feature | Status | What exists |
|---|---|---|
| Conversational AI intake | Delivered as a two-step form with server-side Gemini triage instead of a chat | Form with consent box, validation before the AI call, rate limiting, fallback to NeedsReview |
| Summarisation agent | Not delivered; the case lifecycle was prioritised | The same agent service interface can host it |
| Document checklist per visa type | No API yet | `ChecklistTemplate` table |
| Messages between client and consultant | No API yet; menu items hidden in the web portal | `Message` table |
| Visa expiry reminders for renewals | No API yet | `VisaRecord` table |
| Turning a qualified lead into a case | Not possible from the web portal: there is no endpoint listing visa categories, and consultants cannot look up client accounts | `POST /api/cases` exists for a known client and visa category |
| Visa category on the enquiry form | Not asked yet, although the client named it as one of four essential questions; the AI prompt uses only immigration status and whether help is needed | Needs a visa categories endpoint first |
| Admin reports page | Menu item hidden | Dashboard statistics and CSV export of cases |
| Client profile page | Menu item hidden | None yet |

## 3. Fixed during the project

Issues found by testing or review and fixed before submission.

| Issue | Fix |
|---|---|
| AI verdict was never read: every successful Gemini call marked the lead Qualified, and no reason was saved | The API now reads the verdict and reason, and falls back to NeedsReview (PR #18); the reason is shown to consultants (PR #19) |
| Consultants could export, and change documents on, cases that were not theirs | Ownership checks on export and on document approval, rejection and archiving (PR #18) |
| Archiving a document wrote no audit row | Audit row added (PR #18) |
| Suspending, reinstating and deleting users wrote no audit rows | Audit rows added (PR #20) |
| Admin delete returned 405 | Now a soft delete: the user is marked inactive and kept for the audit trail |
| Password reset accepted any non-blank token, so a password could be changed without access to the email (was ISS-01, Critical) | The reset link now carries a signed token that expires after 30 minutes and names its purpose and email; a made-up token is refused (A15). The user's refresh tokens are revoked after a reset (PR #25) |
| Password reset refused every genuine link, a link could have been reused for 30 minutes, and new passwords had no policy (was ISS-01, High) | `PasswordResetTokenService` reads the claims from the validated token, uses its own `password-reset` audience and a password-hash stamp so each link works once; register and reset apply a NIST-style password policy (12 to 64 characters, common and context-word checks) |
| Behind a hosting proxy, all users would share one sign-in rate limit (was ISS-03) | The API reads the forwarded client address before the rate limiter (PR #25). See the note under ISS-05 below |
| Lead review with an empty body `{}` returned 200 and reset the lead to Pending (was ISS-06) | Verdict is required: 400 and the lead is unchanged (PR #25, A12) |
| Stage change with an empty body would have moved a case back to the first stage, and accepted notes that were never saved | Stage is required: 400 (A17); the unsaved notes field was removed (PR #25). Rest open as [ISS-07](#iss-07) |
| Uploaded test files could be committed | `Strategies.Api/wwwroot/uploads` added to `.gitignore` (PR #18) |
| Seeded documents could not be downloaded: no file existed for any seeded `FilePath` | Placeholder PDFs for all five seeded documents are in `Strategies.Api/SeedFiles` and ship with the API. On every startup, any that are missing are copied into `Storage:UploadsRoot` (existing files are never overwritten), so a fresh or redeployed host needs no manual step |
| Path traversal in document storage (Critical, found in code review): `POST /api/documents` stored any `FilePath` from the request, and the download served `wwwroot/` plus that path, so a signed-in client could register `../appsettings.json` on their own case and download the JWT key and database password; `//etc/passwd` escaped `wwwroot` entirely | Upload returns only the generated `{Guid}{ext}` name. `POST /api/documents` accepts only such a name for a file that exists (otherwise 400 "Invalid file reference."). The download resolves only the file name inside `Storage:UploadsRoot`, checks the canonical path stays inside the root, and otherwise returns 404. Unit, controller and in-process API tests cover the traversal values |
| Sign-in fell back to four built-in accounts, with passwords in the source, when the database was unreachable or the user was not found | Fallback removed: users are read from the database only, and a database outage fails the request without signing anyone in. The demo accounts are unchanged because they come from `database/seed` |
| Known limitation (not fixed): a client who knew another upload's stored file name could register it on their own case and download that file | Impractical, since names are random GUIDs (122 random bits) that only the uploader and staff ever see. A full fix would record the uploader with each stored file and check it in `POST /api/documents` |
| POPIA consent was checked in the browser only: the request carried no consent and the API stored none (was ISS-04, High) | `POST /api/leads` now requires `consent: true`. Without it (missing or false) the API answers 400 with the field error "You need to agree before we can assess your enquiry." and `code: "consent_required"`, before anything is sent to the model or saved. The time of consent is stored on the lead (`ConsentAt`, migration `AddLeadConsentAt`; older leads have none). The enquiry form sends `consent` and shows a returned consent error under the checkbox. Tests: `LeadConsentTests.ConsentLeftOut_Returns400ConsentRequired_AndStoresNothing`, `ConsentFalse_Returns400ConsentRequired_AndStoresNothing`, `ConsentTrue_CreatesTheLead_AndStoresWhenConsentWasGiven`, `LeadsRateLimitTests.RequestsWithoutConsent_StillCountTowardsTheLimit` (the rate limit still applies), web `enquiryServerFieldErrors` |
| Password guessing was limited per address only, and the limit could be avoided by sending a made-up `X-Forwarded-For` header (was ISS-05, High; A19) | Two parts. (1) The API reads `X-Forwarded-For` only when `ForwardedHeaders:Enabled` is true (Production), and then only its last entry, the one Azure's front end appends (`ForwardLimit = 1`); see "Rate limiting and proxies" in the root README and `LeadsRateLimitTests`. (2) Per-account lockout (NFR-1.4): 5 failed sign-ins within 15 minutes lock the account for 15 minutes (`Auth:Lockout:MaxAttempts`, `WindowMinutes`, `DurationMinutes`). While locked, even the correct password is refused with 429, code `account_locked` and a `Retry-After` header. The count lives on the user (`FailedLoginCount`, `FailedLoginWindowStart`, `LockoutEndsAt`, migration `AddUserLockout`); an email that does not exist is counted in memory and answered the same way, after a dummy password check, so lockout does not show which accounts exist. A successful sign-in, a completed password reset or an admin unlock (`PATCH /api/admin/users/{id}/reinstate`) clears it. Audit rows "Account locked" and "Account unlocked" (with the admin's id when an admin unlocked it). Tests: `LoginLockoutTests` (22), `LeadsRateLimitTests.SignInRateLimit_StillLimitsEachIp_NextToTheAccountLockout`, web `signInErrorMessage` |
| Seed data reused the same id pattern across tables | Distinct seed ids (PR #19) |
| First-time visitors triggered a failed token refresh and a console error | The web portal only refreshes when a session exists (PR #19) |
| Admin overview panels were read in a different order from how they look (WCAG 1.3.2, 2.4.3) | Source order now matches the visual order (PR #23) |
| Caps Lock warning was shown only as an icon | Warning given in words again (PR #23) |
| Mobile: a fresh clone could not reach the local API: the debug http setting was never committed, because the root `.gitignore` ignores every `debug/` folder | Debug-only network config (http to `10.0.2.2` and `localhost` only) and a `.gitignore` exception (PR #27) |
| Mobile: a refused staff session was never revoked on the server (the revoke call ran on the main thread and its error was hidden) | Sign-in runs off the main thread; Network Inspector shows `revoke` 200 (PR #27) |
| Mobile: "You have been signed out." and "Incorrect email or password." showed together | A new sign-in clears the old notice (PR #27) |
| Mobile: after a client uploaded a new copy on the web, the app still asked for a replacement and counted both copies | Same rule as the web: a rejected copy is hidden once a newer copy of the same type exists, with unit tests (PR #27) |
| Mobile: told clients to contact their consultant for a new copy | Points to upload on the web portal (PR #27) |
| Mobile: Gradle wrapper missing, so a fresh clone could not build or run the tests | `gradlew`, `gradlew.bat` and the wrapper jar committed (PR #27) |
| Stage change accepted archived cases and unchanged stages (was ISS-07, Low) | `PATCH /api/cases/{id}/stage` now returns 400 "This case is archived and cannot be changed." for archived cases and 400 "The case is already at that stage." when the stage is unchanged, writing no history or audit records. Tests: `CasesControllerStageTests` (5) |
| Mixed audit action wording and old lead verdict not recorded (was ISS-12, Low) | Audit actions for stage updates and lead triage are now stored as sentences ("Changed case stage from X to Y", "Changed lead verdict from X to Y") preserving the previous value. The old verdict is recorded in the Action sentence rather than a separate Detail field: `AuditLog` has no Detail column, and adding one would need a migration. `PATCH /api/leads/{id}/status` returns 400 "The lead already has that verdict." if unchanged, writing no audit row. Tests: `LeadsStatusTests` (4), `CasesControllerStageTests` |
| Refresh tokens kept in memory, so an API restart or redeploy signed everyone out (was ISS-15, Medium) | Refresh tokens are stored hashed (SHA-256) in the `RefreshToken` table (`DbRefreshTokenStore`, migration `AddRefreshTokenTable`, also in `migrate.sql`), with no in-memory fallback. Each refresh rotates the token; reusing an old token more than 30 seconds after its rotation, or after logout, revokes every session of the user. A token sent again within 30 seconds of its rotation (duplicated tab, retry after a lost response) is refused without revoking other sessions. Logout and password reset revoke every session. Tests: `DbRefreshTokenStoreTests`, `AuthControllerTests` |
| Users list could not show the join date (was ISS-13, Low) | Added `CreatedAt` to `UserDto`, populated from `User.CreatedAt` in `AdminController.GetUsers` and `UpdateUser`, and added a "Joined" column to the admin Users page with formatted dates. Tests: `AdminUserTests.GetUsers_ReturnsCreatedAtForEachUser` |
