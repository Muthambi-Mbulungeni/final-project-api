# Screenshots

Screenshots of the web portal for the EXPO deck, the poster board and the [user guide](../USER_GUIDE.md).

## How they were taken

- **Code:** `develop` at commit `98ff559` (web and API unchanged). The 24 screens touched by the UI polish pass were retaken on 22 Oct 2026 from `develop` `765d506` plus the polish commits on `feature/riaad-ui-polish`, the two polish commits on `feature/riaad-messaging-web` (without the message thread itself) and the Users page polish on `feature/riaad-admin-edit-user` (without Edit user). Retaken: `public-02-enquiry-errors-mobile`, `client-01` to `client-06`, `consultant-01` to `consultant-09` and `admin-03` to `admin-10`. The earlier versions are in the history of this folder.
- **Messaging (22 Oct 2026):** `client-01`, `client-02`, `client-06`, `consultant-05` and `consultant-06` were retaken, and `consultant-10` to `consultant-12`, `client-07` to `client-09` and `admin-11` added, from `develop` `aff26b0` (the case message thread, PR #19, is in). The local database was migrated to the seven migrations then on `develop` and its demo messages were replaced with a made-up conversation (13 messages: one case with a back-and-forth, one with a waiting question, an archived case). Everything else was taken as described above, including the same renamed people. Mei Chen (mei.chen@example.com) is the client of the archived case.
- **Build:** production build of `strategies-web` served by `vite preview`, so the development-only role switcher does not appear. `/api` is proxied to the API on `http://localhost:5151`.
- **Data:** a local PostgreSQL database with the five migrations and the demo seed in [`database/seed`](../../database/seed). For the pictures only, three placeholder seed names were changed in that local database: "John Doe" became Lerato Mahlangu, "Consultant User" became Nomvula Zulu and "Riaad" became Pieter van Wyk (pieter.vanwyk@nexus.com). The seed files themselves are unchanged. All people, emails and files are made up.
- **AI triage:** no Gemini key was set, so the new enquiry (Kagiso Molefe) shows the agent's fallback verdict, "Needs review". The seeded leads carry seeded verdicts.
- **Browser:** Microsoft Edge driven by Playwright, light theme, reduced motion, South African locale and time zone. Phone shots emulate a touch screen, so no hover style shows (the first phone enquiry shot had one where the mouse pointer was left).
- **Size:** desktop 1440 × 900, phone 390 × 844, both at 2× pixel density (2880 × 1800 and 780 px wide). Files ending in `-full` and the phone shots show the whole page, not only the first screen.
- **Accounts:** the demo accounts in [`database/seed/README.md`](../../database/seed/README.md) (admin@nexus.com, consultant@nexus.com, client@nexus.com).

## Web

User-guide sections refer to [USER_GUIDE.md](../USER_GUIDE.md). Each deck suggestion is only a suggestion.

### Public

| File | Shows | User guide | Deck / board |
|---|---|---|---|
| `public-01-enquiry.png` | Enquiry form, step 1 "About you" | 2 | Problem and intake |
| `public-01-enquiry-mobile.png` | Same, on a phone | 2 | Mobile-friendly intake |
| `public-02-enquiry-errors.png` | Step 1 validation: missing name, bad email, no status chosen | 2, 6 | Validation |
| `public-02-enquiry-errors-mobile.png` | Same, on a phone | 2, 6 | |
| `public-03-enquiry-step2-errors.png` | Step 2 "Your plans": missing start date and POPIA consent | 2, 6 | POPIA consent |
| `public-03-enquiry-step2-errors-mobile.png` | Same, on a phone | 2, 6 | |
| `public-04-enquiry-step2-filled.png` | Step 2 completed, ready to submit | 2 | |
| `public-05-enquiry-sent.png` | Confirmation with the enquiry reference | 2 | Intake result |
| `public-06-sign-in.png` | Sign-in page with the five-stage journey | 1 | Title or login slide |
| `public-06-sign-in-mobile.png` | Same, on a phone | 1 | |
| `public-07-sign-in-error.png` | Wrong password message | 1, 6 | Security |
| `public-08-forgot-password.png` | Forgot password | 1 | |
| `public-08-forgot-password-mobile.png` | Same, on a phone | 1 | |
| `public-09-reset-password.png` | Choose a new password (from the email link) | 1 | Security |
| `public-09-reset-password-mobile.png` | Same, on a phone | 1 | |
| `public-10-reset-password-errors.png` | Password rule error (12 to 64 characters) | 1, 6 | Validation |
| `public-11-not-found.png` | Branded "page not found" | 6 | |

### Client (Lerato Mahlangu, client@nexus.com)

| File | Shows | User guide | Deck / board |
|---|---|---|---|
| `client-01-dashboard.png` | My case: action needed, five-stage progress, next step; the message thread starts below the fold | 3 Follow your case | Client journey (hero) |
| `client-02-dashboard-full.png` | Same, whole page | 3 Follow your case | |
| `client-03-documents.png` | Documents: rejected medical report with the consultant's note | 3 Replace a rejected document | Document loop |
| `client-04-documents-full.png` | Same, whole page with all documents | 3 Replace a rejected document | |
| `client-05-upload-new-copy.png` | A new copy chosen and ready to send (not sent) | 3 Replace a rejected document | Upload |
| `client-06-dashboard-mobile.png` | My case on a phone, whole page | 3 On your phone | Responsive design |
| `client-07-messages.png` | Message thread on My case: the consultant's questions and the client's replies, with the send box | 3 Message your consultant | Client journey, messaging |
| `client-08-messages-mobile.png` | Same thread on a phone | 3 On your phone | Responsive design |
| `client-09-message-archived.png` | Archived case: the thread stays readable, sending is refused ("This case is archived, so no new messages can be sent.") | 3 Message your consultant | Case lifecycle |

### Consultant (Nomvula Zulu, consultant@nexus.com)

| File | Shows | User guide | Deck / board |
|---|---|---|---|
| `consultant-01-dashboard.png` | Today's caseload: intake agent results, leads waiting, recent cases | 4 Overview | AI triage impact |
| `consultant-02-lead-triage.png` | Lead triage list with the agent's verdicts | 4 Review new enquiries | AI triage |
| `consultant-03-lead-detail.png` | One lead: agent verdict, confirm or override, enquiry details | 4 Review new enquiries | AI with human review |
| `consultant-04-case-list.png` | My cases with stage badges | 4 Manage a case | Case pipeline |
| `consultant-05-case-detail.png` | Case panel: decision banner, stage steps, stage change, documents | 4 Manage a case | Case pipeline (hero) |
| `consultant-06-case-documents.png` | Case panel scrolled to the documents | 4 Review documents | |
| `consultant-07-reject-note-required.png` | Rejecting without a note is blocked | 4 Review documents, 6 | Validation |
| `consultant-08-stage-change.png` | "Under review" chosen for Ayesha Patel's case | 4 Manage a case | State machine |
| `consultant-09-stage-changed.png` | Case moved from Document gathering to Under review, with confirmation message | 4 Manage a case | State machine |
| `consultant-10-case-messages.png` | Case panel scrolled to the message thread: the newest client reply is tagged New | 4 Message the client | Messaging |
| `consultant-11-unread-before.png` | Overview with the bell showing 2 unread messages | 4 Message the client | Notifications |
| `consultant-12-unread-after.png` | Same page after the thread was opened: the count is 1 (the other unread message is on another case) | 4 Message the client | Notifications |

### Admin (System Admin, admin@nexus.com)

| File | Shows | User guide | Deck / board |
|---|---|---|---|
| `admin-01-overview.png` | System overview: totals, consultant workload, users | 5 Overview | Real-data dashboard |
| `admin-02-overview-full.png` | Same, whole page with recent activity | 5 Overview | |
| `admin-03-users.png` | Users list with roles and status | 5 Manage users | Role-based access |
| `admin-04-suspend-confirm.png` | Confirm suspending a user | 5 Manage users | |
| `admin-05-suspended.png` | User suspended, with confirmation message | 5 Manage users | |
| `admin-06-leads.png` | All leads with the agent's verdicts and filters | 5 Leads and export | AI triage |
| `admin-07-lead-ai-triage.png` | New enquiry routed to review by the agent | 5 Leads and export | AI fallback |
| `admin-08-cases-export.png` | All cases with consultant column and Export CSV | 5 Assign a consultant, Leads and export | Export |
| `admin-09-export-done.png` | CSV export finished | 5 Leads and export | Export |
| `admin-10-audit-log.png` | Audit log: who changed what, and when | 5 Read the audit log | Audit trail / POPIA |
| `admin-11-case-messages-read-only.png` | The same thread in an administrator's case panel: readable, no send box, with a note saying why | 5 Cases | Role-based access |

## Not captured

| Requested | Why |
|---|---|
| Register page | The web portal has no registration page; client accounts are not self-registered. |
| Client case view (separate page) | The client's case is shown on "My case" (`client-01`), there is no separate case page. |
| Notifications page | Not built in the web portal; only the bell count in the header shows. |
| Mobile app | No Android SDK, emulator or device on the capture PC. |
