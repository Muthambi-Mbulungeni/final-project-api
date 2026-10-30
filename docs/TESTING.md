# Test plan and results

Strategies Migration Platform, Team Nexus Solutions (INSY7315).

This document covers the API, the web portal and the database seed. The Android app has its own log: [strategies-mobile/TESTING.md](../strategies-mobile/TESTING.md). Defects found during testing are tracked in [ISSUES.md](ISSUES.md).

## 1. Summary

| Level | Tool | Result | Where it was run |
|---|---|---|---|
| API unit tests | xUnit (`Strategies.Api.Tests`) | **27 of 27 passed** (25 test methods) | Local, develop `0844264`, 2026-10-05 |
| Web unit tests | Vitest (`strategies-web`) | **185 of 185 passed** (19 files) | Local, develop `0844264`, 2026-10-05 |
| Web lint and production build | ESLint, Vite | Passed | CI, every push and PR |
| Continuous integration | GitHub Actions (`.github/workflows/ci.yml`) | **Passed** on develop `0844264` ([run 37273291628](https://github.com/EMGPRS/insy7315-2026-task-2-hartmarcost10462493/actions/runs/37273291628)) | GitHub |
| API checks against a real database | Scripted HTTP calls | Passed, except the defects in ISSUES.md | Local PostgreSQL 17, seeded |
| Browser checks per role | Edge and Opera, DevTools at 1280 px and 390 px | Passed | Local, real API and seeded data |
| Responsive and console sweep | Playwright driving Edge | 16 screens at 1280 and 390 px: no horizontal overflow, no console errors when signed in | Local, 2026-10-04 |
| Android app, manual end to end | Emulator, real API ([mobile log](../strategies-mobile/TESTING.md)) | 8 of 12 checks passed against the real API and seeded database, 4 not yet run; unit tests 19 of 19 | Kundai's emulator; re-run by Riaad, 5 Oct 2026 |

## 2. Requirement traceability

Each requirement from the brief, with the tests that prove it. Test IDs refer to sections 5 to 7.

| Requirement | Evidence | Status |
|---|---|---|
| Different dashboards per role | B3 (client), B6 to B8 (consultant), B9 to B11 (admin) | Pass |
| Role-based access on the server | A3 (consultant sees own cases only), A11 (client gets 403) | Pass |
| JWT authentication and refresh | `TokenServiceTests`, `AuthServiceTests` (refresh and revoke), A1, B3 | Pass |
| Passwords hashed with bcrypt | Login tests verify against stored bcrypt hashes (`PasswordHasherService`, work factor 11) | Pass |
| Brute-force protection (NFR-1.4) | A2 (5 sign-ins per minute per address), A19, A20; `LoginLockoutTests` (lock after 5 failed sign-ins in 15 minutes, correct password refused while locked, unlock after 15 minutes, success and password reset and admin unlock clear it, unknown email answered the same way) | Pass: per-address rate limit and per-account lockout (was [ISS-05](ISSUES.md#fixed-during-the-project)) |
| Password reset | A15, A16 | Made-up tokens refused (A15), but **genuine reset links are refused too** (A16, [ISS-01](ISSUES.md#iss-01)) |
| Client and server validation | B1, B5 (client side); A4, A6, A7, A10, A11 (server side); `validation` tests | Pass |
| File upload with server validation | A6, A7, B4, B5 | Pass |
| Audit trail | A5, A13, B10 | Pass |
| Search, filter, sort, pagination | A8, A9, B11; `listData` and `table` tests | Pass |
| Dashboard on real data | B3, B9 | Pass |
| Export | Cases page CSV export | Not yet tested |
| Notifications | `notifications` tests; mobile check 4 | Pass |
| Soft delete | Admin user delete marks the user inactive instead of deleting | Not yet tested |
| AI triage with fallback | A14 (no key: lead saved as NeedsReview) | Pass for fallback; live Gemini call not tested ([ISS-08](ISSUES.md#iss-08)) |
| POPIA consent | B1 (enquiry cannot be sent without consent) | Pass. The API also refuses an enquiry without consent (400, code `consent_required`) and stores when consent was given (was ISS-04, fixed; `LeadConsentTests`) |
| Responsive and accessible | Section 7 | Pass |
| Deployment | Hosted URLs | Not yet tested |

## 3. Test plan

### 3.1 Scope

In scope: authentication and sessions, role-based access (Admin, Consultant, Client), the public enquiry form and AI triage fallback, lead review, case stages, document upload and review, admin user management, the audit trail, list search, filter, sort and pagination, and layout on desktop and phone.

Out of scope for this submission: load and performance testing, a live Gemini call (no API key in the test environment, so the fallback path was tested instead), and penetration testing beyond the checks listed here.

### 3.2 Levels

1. **Unit tests** run on every push and pull request in CI. By team rule, nobody pushes directly to `develop`: work goes through a pull request and is merged only after CI passes.
2. **API checks** call the running API with real HTTP requests against a seeded PostgreSQL database. They check status codes, validation messages and the rows written to the audit log.
3. **Browser checks** sign in with the seeded accounts, one role at a time, at 1280 px (desktop) and 390 px (phone).
4. **Regression proof for new tests.** When a fix comes with new tests, the fix is undone temporarily, the new tests must fail, and then the fix is restored. This shows that the tests actually catch the bug.

### 3.3 Environment

| Item | Version or value |
|---|---|
| API | .NET 10, EF Core, `dotnet run --launch-profile http` on port 5151 |
| Database | PostgreSQL 17, all migrations applied, demo data from [database/seed](../database/seed/README.md) |
| Web | Node 22, React 19, Vite 8, dev server on port 5173 (proxies `/api` to the API) |
| Browsers | Microsoft Edge, Opera (Chromium) |
| Accounts | Seeded demo accounts per role; passwords are in [database/seed/README.md](../database/seed/README.md) |

### 3.4 Entry and exit criteria

- **Entry:** the branch builds; the database is migrated and seeded.
- **Exit (per pull request):** all unit tests pass, lint and build pass, and CI is green. New behaviour has been checked in a browser at both widths. Any defect that remains open is logged in ISSUES.md.

## 4. Automated tests

### 4.1 API (xUnit, 27 cases)

| Area | What is tested |
|---|---|
| Login (`AuthServiceTests`) | Valid credentials return tokens; a wrong password, an unknown email or an inactive user is refused; email matching ignores case |
| Refresh tokens (`AuthServiceTests`) | A valid token issues new tokens; revoked and garbage tokens are refused; revoking works once only |
| JWT (`TokenServiceTests`) | The access token carries the right subject and role, and its expiry is in the future; refresh tokens are URL-safe, unique and long enough; the service refuses to start without a JWT key |
| Case lifecycle (`CaseManagementServiceTests`) | Each of the five stages advances to the next one; FinalDecision does not advance; `UpdatedAt` is refreshed; an unknown case returns false |

Run: `dotnet test` from the repository root.

**Known gap.** These tests cover the services only. No automated test calls the API controllers, so role checks, validation responses and audit rows are not tested automatically. They are covered by the scripted API checks in section 5, which have to be repeated by hand. The next step is integration tests using `WebApplicationFactory` against a test database, so that section 5 runs in CI.

### 4.2 Web (Vitest, 185 tests in 19 files)

The tests cover the logic behind the pages, kept in `src/lib` and `src/config` so that it can be tested without a browser:

| Area | Test files |
|---|---|
| Sign-in, sessions, token refresh, idle sign-out | `auth`, `authRestore`, `authMessages`, `session`, `useIdleSignOut`, `api` |
| Client and server validation messages | `validation`, `api` |
| Role navigation | `navigation`, `listPaths` |
| Enums shared with the API (index or name) | `enums` |
| Cases: stages, stage markers, consultant workload | `cases` |
| Leads: verdicts and the agent's triage reason | `leads` |
| Documents: upload rules (type, 5 MB), replaced copies, review notes | `documents`, `documentReview` |
| Audit log wording and short ids | `audit` |
| Search, filter, sort and pagination parameters | `listData`, `table` |
| Formatting and notifications | `format`, `notifications` |

Run: `cd strategies-web`, then `npm ci` and `npm test`.

## 5. API checks against a real database

Run against the local API with the seeded database, 2026-10-03 and 2026-10-04. A12 and A15 were re-run, and A16 to A19 added, on develop `e2751ac` (after the final API patches in PR #25), 2026-10-05.

| # | Check | Expected | Result |
|---|---|---|---|
| A1 | Sign in through the web proxy (each role) | 200 with tokens | Pass |
| A2 | Six failed sign-ins within a minute from one address | 429 (rate limit, 5 per minute) | Pass |
| A3 | Consultant lists cases | Only their own cases, with client and consultant names | Pass (5 cases) |
| A4 | Reject a document without a note | 400 | Pass |
| A5 | Reject a document with a note | 2xx, audit row "Rejected document" | Pass |
| A6 | Upload a `.docx` file | 400 "Invalid file type. Only PDF, JPG, and PNG are allowed." | Pass |
| A7 | Upload a 6 MB PDF | 400 "File size exceeds the 5MB limit." | Pass |
| A8 | Leads page 1, page 2, total | 10, 1, 11 | Pass |
| A9 | Leads filtered by status Qualified | 5 leads, all Qualified | Pass |
| A10 | Change a case stage, then an invalid stage | 200, then 400 | Pass |
| A11 | Lead review: valid verdict, invalid verdict, unknown lead, client role | 200, 400, 404, 403 | Pass |
| A12 | Lead review with an empty body `{}` | 400 | Pass: 400, lead unchanged (failed before PR #25) |
| A13 | Admin suspends and reinstates a user | Audit rows "Suspended user" and "Reinstated user" | Pass |
| A14 | Public enquiry with no Gemini key configured | Lead is saved as NeedsReview (AI fallback) | Pass |
| A15 | Password reset only with a valid reset token | Request refused without a valid token | Pass: 400 for a made-up token and for a token issued for another email (failed before PR #25) |
| A16 | Password reset with a valid token, made by the same code and key the API uses | 200 | **Fail**: 400 "Invalid token claims" ([ISS-01](ISSUES.md#iss-01)) |
| A17 | Stage change with an empty body `{}` | 400 | Pass |
| A18 | Stage change to the stage the case already has, or on an archived case | 400 | Pass: 400, no history or audit row written (was [ISS-07](ISSUES.md#fixed-during-the-project)) |
| A19 | Seven failed sign-ins sent straight to the API, each with a different `X-Forwarded-For` header | 429 after 5 | Was **Fail** (no 429); fixed: the header is read only behind the configured proxy (`LeadsRateLimitTests`) |
| A20 | Five wrong passwords for `client@nexus.com`, then the right one (local API) | 5th answer 429 `account_locked` with `Retry-After: 900`; the right password is still refused, with the time left | Pass |

## 6. Browser checks per role

Seeded accounts, real API, DevTools device mode at 1280 px and 390 px.

| # | Role | Journey | Result |
|---|---|---|---|
| B1 | Visitor | Enquiry form: invalid fields block the submit and focus the first one; consent is required; success message | Pass |
| B2 | Visitor | Sign-in page with a first-time visitor: no token refresh call, no console error | Pass |
| B3 | Client | Dashboard shows the case stage and next step; reloading keeps the session | Pass |
| B4 | Client | Documents: a rejected document shows the reviewer's note and an upload form; after a new copy is uploaded it shows Awaiting review | Pass |
| B5 | Client | Upload errors for a file over 5 MB and for an empty file | Pass |
| B6 | Consultant | Lead triage: open a lead, read the agent's reason, mark it qualified or unqualified, change the verdict | Pass |
| B7 | Consultant | Case panel: approve a document; rejecting requires a note; open the file in a new tab | Pass |
| B8 | Consultant | Change a case stage under a stage filter: the panel stays open and the list updates | Pass |
| B9 | Admin | Overview with consultant workload; assign a case to an active consultant | Pass |
| B10 | Admin | Users: suspend and reinstate with confirmation messages; audit log shows names and actions in plain words | Pass |
| B11 | Admin | Leads page: summary counts, paging, filters | Pass |
| B12 | All | Unknown address shows the branded 404 page with a way back | Pass |

## 7. Accessibility and responsive checks

| Check | Result |
|---|---|
| No horizontal scrolling at 390 px on 16 signed-in screens (Playwright, Edge) | Pass |
| Every control reachable by keyboard with a visible focus ring, including the styled file picker | Pass |
| Dialogs and the phone menu keep focus inside while open and return it on close | Pass |
| Reading and tab order match the visual order on the admin overview | Pass (fixed 2026-10-04, WCAG 1.3.2 and 2.4.3) |
| Animations respect "reduce motion" | Pass |
| Caps Lock warning on password fields is given in words, not only an icon | Pass |

## 8. Defects found by testing

Defects found during these tests are recorded with their status in [ISSUES.md](ISSUES.md). Defects in the web portal were fixed before merging. Defects in the API were reported to the API owner.

## 9. How to repeat these tests

1. Start PostgreSQL, apply the migrations and run the seed files ([database/seed/README.md](../database/seed/README.md)).
2. Start the API: `cd Strategies.Api`, then `dotnet run --launch-profile http`
3. Start the web portal: `cd strategies-web`, then `npm run dev`, and open http://localhost:5173
4. Run the automated tests: `dotnet test` from the repository root, and `npm test` in `strategies-web`
5. Follow sections 5 and 6 with the seeded accounts.
