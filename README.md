# Strategies Migration Platform

Case management for **Strategies Migration Services**, a South African immigration consultancy. Built by Team Nexus Solutions for INSY7315 (Work Integrated Learning).

**Demo video:** https://youtu.be/aw8wSutQ7Z8

**Presentation:** [Task 2 presentation (PDF)](docs/presentation/Task2-Presentation-Nexus-Solutions.pdf) · [Implementation access and links (PDF)](docs/presentation/Task2-Implementation-Access-and-Links.pdf)

| Live system | Address |
|---|---|
| Web portal | https://emgprs.github.io/insy7315-2026-task-2-hartmarcost10462493/ |
| API | To be added |

## The problem and the solution

About 80% of the consultancy's web enquiries were junk that staff had to sort by hand, and clients had no way to see how far their application had gone.

The platform has three parts:

1. **Public enquiry form.** Anyone can send an enquiry after giving POPIA consent. The API validates it and asks a Gemini triage agent whether the lead is qualified. If the AI is unavailable or unsure, the lead goes to a consultant as NeedsReview, so no enquiry is lost.
2. **Staff portal.** Consultants review the leads the AI could not decide, manage their cases and approve or reject documents. Administrators manage users, assign cases to consultants for an even workload and read the audit trail.
3. **Client tracking.** Clients follow their case through five stages on the web or the Android app, and upload a new copy of any document a consultant rejects on the web portal.

The five stages: **Initial intake → Document gathering → Under review → Submitted to authority → Final decision**.

## Screenshots

The web portal running against the real API and the demo data, from the production build (5 October 2026). Each caption names what the screen proves and the matching check in [TESTING.md](docs/TESTING.md). Select an image to see it full size.

| | |
|---|---|
| <a href="docs/images/enquiry.png"><img src="docs/images/enquiry.png" width="420" alt="Public enquiry form, step 1 of 2"></a><br>**Visitor: enquiry form.** Two steps, validated in the browser and on the server; POPIA consent on step 2 (B1). | <a href="docs/images/client-case.png"><img src="docs/images/client-case.png" width="420" alt="Client case page with the five stages and an action needed card"></a><br>**Client: my case.** The five-stage progress, what happens next, and a rejected document flagged as Action needed (B3). |
| <a href="docs/images/client-documents.png"><img src="docs/images/client-documents.png" width="420" alt="Client documents page with a rejected document, the consultant's note and an upload form"></a><br>**Client: documents.** The consultant's rejection note and a server-validated upload of a new copy, PDF, JPG or PNG up to 5 MB (B4, B5). | <a href="docs/images/client-case-phone.png"><img src="docs/images/client-case-phone.png" width="200" alt="Client case page at phone width"></a><br>**Client on a phone (390 px).** The same page at phone width, with no sideways scrolling (section 7). |
| <a href="docs/images/consultant-lead.png"><img src="docs/images/consultant-lead.png" width="420" alt="Consultant lead triage with a lead's details open"></a><br>**Consultant: lead triage.** A lead the AI could not qualify, routed for review; the consultant confirms or overrides the verdict (B6). | <a href="docs/images/consultant-case.png"><img src="docs/images/consultant-case.png" width="420" alt="Consultant case panel with stages and a document awaiting review"></a><br>**Consultant: case panel.** Stage control, a document awaiting a decision, Open file, Approve and Reject with a required note (B7, B8). |
| <a href="docs/images/admin-overview.png"><img src="docs/images/admin-overview.png" width="420" alt="Administrator overview with totals and consultant workload"></a><br>**Administrator: overview.** Live totals, enquiries waiting for review, and each consultant's workload against a target of 10 active cases (B9). | <a href="docs/images/admin-audit-log.png"><img src="docs/images/admin-audit-log.png" width="420" alt="Audit log listing actions, records and who performed them"></a><br>**Administrator: audit log.** Who changed what, and when: document decisions, stage changes, assignments, lead reviews and suspensions (B10). |

### Android app (client role)

The Android app on an emulator (API 37.1 image, 1080 × 2400) against the real API and the demo data on a local database, signed in as the demo client John Doe (5 October 2026). It shows the same case as the web screenshots above. Numbers in brackets are checks in the [mobile test log](strategies-mobile/TESTING.md).

| | | | | |
|---|---|---|---|---|
| <a href="docs/images/mobile-my-case.png"><img src="docs/images/mobile-my-case.png" width="160" alt="Android app: my case with an action needed card and the five-stage progress"></a><br>**My case.** Stage 3 of 5 and the document to replace (1, 2). | <a href="docs/images/mobile-documents.png"><img src="docs/images/mobile-documents.png" width="160" alt="Android app: documents with the consultant's rejection note and 1 of 3 approved"></a><br>**Documents.** The consultant's note; new copies are uploaded on the web portal (3). | <a href="docs/images/mobile-messages.png"><img src="docs/images/mobile-messages.png" width="160" alt="Android app: messages with one read and one unread"></a><br>**Messages.** Opening a message marks it read on the server (4, 12). | <a href="docs/images/mobile-signin.png"><img src="docs/images/mobile-signin.png" width="160" alt="Android app: sign-in screen after signing out"></a><br>**Sign in.** Shown after signing out (10). | <a href="docs/images/mobile-staff-refused.png"><img src="docs/images/mobile-staff-refused.png" width="160" alt="Android app refusing an administrator account"></a><br>**Staff refused.** Staff use the web portal; their session is revoked (6). |

## Components

| Component | Folder | Technology | Owner | Status |
|---|---|---|---|---|
| API | [`Strategies.Api`](Strategies.Api) | .NET 10, EF Core, JWT, bcrypt, Gemini | Marco (API, AI), Kaan (security) | Working; open issues in [ISSUES.md](docs/ISSUES.md) |
| Database | [`database`](database/seed/README.md), EF migrations | PostgreSQL | Lekabe | 3 migrations, demo seed data |
| Web portal | [`strategies-web`](strategies-web/README.md) | React 19, Vite, Tailwind, React Router | Riaad | All three roles working against the real API |
| Android app | [`strategies-mobile`](strategies-mobile/README.md) | Kotlin, Jetpack Compose | Kundai | Client role working against the real API: sign-in, case progress, documents, messages; no upload in the app yet |
| CI and deployment | [`.github/workflows`](.github/workflows/ci.yml) | GitHub Actions | Mbulungeni | CI on every push and pull request |

## Architecture

```mermaid
flowchart LR
  subgraph Users
    V[Visitor]
    C[Client]
    S[Consultant / Admin]
  end
  W["Web portal<br/>React SPA, static host"]
  M["Android app<br/>Kotlin, Compose"]
  A["API<br/>.NET 10, JWT auth,<br/>rate limiting, audit log"]
  D[(PostgreSQL)]
  F[["Uploaded files<br/>API server disk"]]
  G["Gemini<br/>triage agent"]

  V -->|enquiry| W
  C --> W
  C --> M
  S --> W
  W -->|"/api over HTTPS"| A
  M -->|"HTTPS, JWT"| A
  A --> D
  A --> F
  A -->|"enquiry details"| G
  G -.->|"verdict and reason<br/>(fallback: NeedsReview)"| A
```

- The web portal calls the API through relative `/api` paths (a dev proxy locally, a host rewrite in production), so the browser never needs CORS.
- Only the API talks to Gemini; the key never reaches a browser or phone.
- Staff changes are recorded in an audit trail: case creation, stage changes and assignments, document reviews and archiving, lead reviews, and user suspensions, reinstatements and deletions.
- Uploaded files are validated on the server (PDF, JPG or PNG, 5 MB at most) and stored on the API server's disk. Cloud storage was planned: see [ISS-09](docs/ISSUES.md#iss-09).

## Quick start (local)

### Prerequisites

- .NET 10 SDK and the EF Core tool: `dotnet tool install --global dotnet-ef`
- PostgreSQL (tested with version 17)
- Node.js 22 and npm
- Android Studio (for the mobile app only)

### 1. Database

You can spin up the database instantly using Docker Compose (recommended):
```bash
docker compose up -d
```
Then, apply the EF Core migrations to create the tables:

```bash
cd Strategies.Api
dotnet ef database update
```

To load the demo data, follow the commands in [database/seed/README.md](database/seed/README.md).

### 2. API

Secrets are kept out of the repository with .NET user secrets ([ISS-02](docs/ISSUES.md#iss-02) lists values that still need to move out of `appsettings.json`). 
For a quick local start, you can just use the defaults already in `appsettings.json` (which match the Docker Compose setup), so you only need to run:

```bash
cd Strategies.Api
dotnet run --launch-profile http
```

The API listens on http://localhost:5151 and, in Development, serves Swagger at http://localhost:5151/swagger

| Setting | Purpose | Required |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection | Yes |
| `Jwt:Key` | Signs access tokens | Yes |
| `Jwt:Issuer`, `Jwt:Audience` | Token issuer and audience | Defaults in `appsettings.json` |
| `Gemini:ApiKey` | AI triage | No: without it every enquiry is saved as NeedsReview |
| `Gemini:Url` | Gemini model endpoint | Default in `appsettings.json` |

### 3. Web portal

```bash
cd strategies-web
npm ci
npm run dev
```

Open http://localhost:5173 (requests to `/api` are forwarded to the API on port 5151).

### 4. Android app

Open `strategies-mobile` in Android Studio and run the `app` configuration on an emulator. Debug builds call the API at `http://10.0.2.2:5151/`. See [strategies-mobile/README.md](strategies-mobile/README.md).

### 5. Sign in

Demo accounts for each role (`admin@nexus.com`, `consultant@nexus.com`, `client@nexus.com`) and their passwords are listed in [database/seed/README.md](database/seed/README.md). They are demo data only.

## Tests and quality

| Part | Command | Result on `develop` |
|---|---|---|
| API | `dotnet test` (repository root) | 27 of 27 passed |
| Web | `cd strategies-web`, then `npm test`, `npm run lint`, `npm run build` | 185 of 185 passed, lint and build clean |
| Mobile | `cd strategies-mobile`, then `./gradlew testDebugUnitTest` | 19 of 19 passed; manual checks in the [mobile test log](strategies-mobile/TESTING.md) |

CI runs the API build and tests and the web lint, tests and build on every push and pull request. The full test plan, requirement traceability and manual results are in [docs/TESTING.md](docs/TESTING.md).

Team rules: you must verify your work. All tests must pass before code is considered for integration; broken builds and untested logic are rejected. Branch from `develop` (`feature/<name>-<task>`), open a pull request, and merge only when CI is green. Never push to `main` or `develop` directly. Commit messages follow Conventional Commits, for example `feat(web): ...`.

## AI: planned and delivered

| Planned | Delivered |
|---|---|
| Conversational intake agent | A structured two-step enquiry form with POPIA consent, validated before any AI call |
| Triage agent | Delivered: Gemini returns a verdict and a reason, which consultants see; any failure falls back to NeedsReview |
| Document checklist agent | Checklist templates exist in the database; no API yet |
| Summarisation agent | Not delivered; the case lifecycle was prioritised |

Details and the other features not delivered are in [ISSUES.md section 2](docs/ISSUES.md#2-planned-but-not-delivered).

## Documentation

| Document | What it covers |
|---|---|
| [Task 2 presentation](docs/presentation/Task2-Presentation-Nexus-Solutions.pdf) | Presentation deck: requirements, architecture, security, DevOps, demo order |
| [Implementation access and links](docs/presentation/Task2-Implementation-Access-and-Links.pdf) | Live links, demo accounts and a guided tour per role |
| [docs/USER_GUIDE.md](docs/USER_GUIDE.md) | Step-by-step guide for administrators, consultants and clients |
| [docs/TESTING.md](docs/TESTING.md) | Test plan, requirement traceability and results |
| [docs/ISSUES.md](docs/ISSUES.md) | Open issues, features not delivered, issues fixed |
| Swagger | Live API reference at `/swagger` when the API runs in Development |
| [strategies-web/README.md](strategies-web/README.md) | Web portal design, sessions, accessibility, deployment |
| [strategies-mobile/README.md](strategies-mobile/README.md) | Android app setup and session handling |
| [database/seed/README.md](database/seed/README.md) | Migrations, demo data and demo accounts |

## Team Nexus Solutions

| Member | Student number | Role |
|---|---|---|
| Marco Alexandre George Hart | ST10462493 | Team lead, API and AI |
| Kaan Dogan | ST10442156 | Security: authentication, rate limiting, file storage |
| Lekabe Herman Mabalane | ST10278399 | Database and migrations |
| Kundai Makuzha | ST10251297 | Android app |
| Mbulungeni Kenneth Muthambi | ST10447819 | Repository, CI, QA and deployment |
| Riaad Hercules | ST10258492 | Web portal, integration testing, documentation |
