# Issue log

Strategies Migration Platform, Team Nexus Solutions (INSY7315).

Known issues at the time of submission, checked against the code on `develop` (`e2751ac`, 2026-10-05, after the final API patches in PR #25). Test IDs (A1, B1 and so on) refer to [TESTING.md](TESTING.md).

**Severity:** Critical = a security risk or data loss. High = a rubric requirement is missing or weakened. Medium = wrong behaviour with a workaround. Low = wording or polish.

**How issues are handled.** Issues are found by the tests in TESTING.md or by reviewing the code. Each one is checked in the code, reported to the owner of that part of the system with a suggested fix, and checked again after the fix is merged. Fixed issues move to section 3.

## 1. Open issues

| ID | Severity | Issue | Owner |
|---|---|---|---|
| [ISS-02](#iss-02) | High | Development secrets committed in `appsettings.json` | Kaan, Mbulungeni |
| [ISS-04](#iss-04) | High | POPIA consent is checked in the browser only, never sent to or stored by the API | Marco, Lekabe |
| [ISS-05](#iss-05) | High | No per-account lockout after failed sign-ins | Kaan |
| [ISS-08](#iss-08) | Medium | Gemini model name may be retired; live call not tested | Marco |
| [ISS-09](#iss-09) | Medium | Uploaded files are stored on the API server's disk | Kaan, Mbulungeni |
| [ISS-10](#iss-10) | Medium | No automated tests for the API controllers | Marco, Mbulungeni |
| [ISS-11](#iss-11) | Medium | Mobile: 4 manual checks not run; release build has no API address | Kundai |
| [ISS-15](#iss-15) | Medium | Refresh tokens are kept in memory, so an API restart signs everyone out | Kaan, Marco |
| [ISS-07](#iss-07) | Low | Stage change accepts archived cases and unchanged stages | Marco |
| [ISS-12](#iss-12) | Low | Mixed audit action wording; old lead verdict not recorded | Marco |
| [ISS-13](#iss-13) | Low | Users list cannot show the join date | Marco, Riaad |
| [ISS-14](#iss-14) | Low | Client logo is low resolution | Riaad |

### ISS-02

**High. Development secrets committed in `appsettings.json`.**

- **Problem:** `Strategies.Api/appsettings.json` contains a database password and the JWT signing key.
- **Evidence:** `ConnectionStrings:DefaultConnection` and `Jwt:Key` in that file.
- **Impact:** if a hosted deployment uses these values, anyone with access to the repository can sign valid tokens for any role.
- **Fix:** remove the values from the file, set them as environment variables (or user secrets locally) on each host, and rotate the JWT key and the database password.

### ISS-04

**High. POPIA consent is checked in the browser only.**

- **Problem:** the enquiry form requires the consent box, but the request it sends has no consent field, and the API has no field or column for it.
- **Evidence:** B1; `EnquiryPage.jsx` request body; no consent field in `CreateLeadRequest` or the `Lead` table.
- **Impact:** there is no record that a person consented to processing, and a request sent straight to the API skips the check.
- **Fix:** add a required consent flag to `CreateLeadRequest` (reject the request without it) and store the consent time on the lead (a new migration); the web form then sends it.

### ISS-05

**High. No per-account lockout after failed sign-ins.**

- **Problem:** the only protection against password guessing is the per-address rate limit (A2), which does not stop guesses spread across many addresses. Since PR #25 the API takes the address from the `X-Forwarded-For` header and trusts that header from any sender. Behind a hosting proxy that adds the real address, this is correct. If the API can be reached directly, a caller can put a different address in each request and is never limited (A19).
- **Evidence:** no lockout fields or logic in the `User` model or `AuthService`; `ForwardedHeadersOptions` in `Program.cs` clears `KnownProxies` and `KnownIPNetworks`.
- **Fix:** count failed sign-ins per user, lock the account for a set time after, for example, 5 failures, and reset the count after a successful sign-in. Make sure the hosted API is only reachable through its proxy, or list the host's proxy addresses in `ForwardedHeadersOptions`.

### ISS-08

**Medium. Gemini model name may be retired; live call not tested.**

- **Problem:** the API called `gemini-1.5-flash`, which may no longer be available. The test environment has no Gemini key, so a live call was not tested.
- **Evidence:** `Gemini:Model` in `appsettings.json`; A14 shows the fallback.
- **Impact:** if the call fails, every enquiry is saved as NeedsReview, so no enquiry is lost but none is triaged automatically.
- **Fix:** the code fix is done. The model is now set by `Gemini:Model` (`gemini-3.8-flash-lite`), the key is sent in the `x-goog-api-key` header, the call has a 10-second timeout, instructions are in `systemInstruction` with applicant data treated as untrusted, name and email are not sent, the reply is checked against a JSON schema, and every run is logged to `AgentRun`. Unit tests in `GeminiAgentServiceTests.cs` cover each fallback path. **Remaining:** test one real call with a key.

### ISS-09

**Medium. Uploaded files are stored on the API server's disk.**

- **Problem:** uploads are saved to `wwwroot/uploads` on the API server, not to cloud storage (R2 was planned).
- **Evidence:** `DocumentsController` upload and file endpoints.
- **Impact:** on a host whose disk is reset on each deploy, files would be lost while their database rows remain.
- **Fix:** move uploads to R2 or another object store, or use a host with a persistent disk.

### ISS-10

**Medium. No automated tests for the API controllers.**

- **Problem:** the API tests cover services only, so role checks, validation responses and audit rows are tested by hand (TESTING.md section 5).
- **Evidence:** `Strategies.Api.Tests`.
- **Fix:** integration tests with `WebApplicationFactory` against a test database, run in CI.

### ISS-11

**Medium. Mobile: 4 manual checks not run; release build has no API address.**

- **Problem:** checks 7 (rate limit), 8 (reopen app), 9 (expired access token) and 11 (API stopped) are not yet run. Release builds point at `https://CHANGE-ME.example.com/` because the API is not hosted yet.
- **Evidence:** [mobile test log](../strategies-mobile/TESTING.md); `strategies-mobile/app/build.gradle.kts`.
- **Fix:** run the 4 checks; set the hosted API address once deployed.

### ISS-15

**Medium. Refresh tokens are kept in memory.**

- **Problem:** the API stores refresh tokens in an in-memory store, not in the database.
- **Evidence:** `Program.cs` registers `InMemoryRefreshTokenStore` as `IRefreshTokenStore`.
- **Impact:** every API restart or redeploy, and every wake-up of a free host that sleeps when idle, forgets all refresh tokens. Users are signed out when their access token next expires. With more than one API instance, a token issued by one instance is unknown to the others.
- **Fix:** store refresh tokens (hashed) in a database table behind the same `IRefreshTokenStore` interface.

### ISS-07

**Low. Stage change accepts archived cases and unchanged stages.**

PR #25 fixed the empty body (section 3).

- **Problem:** `PATCH /api/cases/{id}/stage` still changes archived cases. Sending the stage a case already has returns 200 and writes a history row and an audit row for a change that did not happen.
- **Evidence:** A18; `CasesController.UpdateStage` has no archived or same-stage check.
- **Impact:** the case history can show stage changes that never happened. The web portal disables the current stage in its stage list.
- **Fix:** return 400 for archived cases and for unchanged stages.

### ISS-12

**Low. Mixed audit action wording; old lead verdict not recorded.**

- **Problem:** some audit actions are stored as code names (`UpdateCaseStage`, `UpdateLeadStatus`), others as sentences ("Suspended user"). The lead review audit row does not record the previous verdict, and sending the verdict a lead already has returns 200 and writes another audit row.
- **Evidence:** `CasesController`, `LeadsController`.
- **Fix:** store sentences and include the old value. The web audit page already shows the code names in plain words.

### ISS-13

**Low. Users list cannot show the join date.**

- **Problem:** the API's user data (`UserDto`) has no creation date, so the admin Users list cannot show it.
- **Fix:** add `CreatedAt` to `UserDto`; the web column follows.

### ISS-14

**Low. Client logo is low resolution.**

- **Problem:** the logo is a small PNG without transparency and looks soft on high-resolution screens.
- **Fix:** ask the client for an SVG or a 2x transparent PNG.

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
| Behind a hosting proxy, all users would share one sign-in rate limit (was ISS-03) | The API reads the forwarded client address before the rate limiter (PR #25). See the note in [ISS-05](#iss-05) |
| Lead review with an empty body `{}` returned 200 and reset the lead to Pending (was ISS-06) | Verdict is required: 400 and the lead is unchanged (PR #25, A12) |
| Stage change with an empty body would have moved a case back to the first stage, and accepted notes that were never saved | Stage is required: 400 (A17); the unsaved notes field was removed (PR #25). Rest open as [ISS-07](#iss-07) |
| Uploaded test files could be committed | `Strategies.Api/wwwroot/uploads` added to `.gitignore` (PR #18) |
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
