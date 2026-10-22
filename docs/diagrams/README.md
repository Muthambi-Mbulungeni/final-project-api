# Report diagrams

Mermaid sources (`.mmd`) drawn from the code on `develop` (98ff559), except `seq-password-reset` and `state-user-account`, which follow `origin/main` (499c39d, single-use reset tokens and password policy; that commit is not on develop yet). Each is rendered to `.svg` and to a 3x `.png` on a white background for the Word/PDF report. Section numbers refer to the INSY7315 module manual, Task 1 documentation (9.1).

Dashed boxes mean one of two things, and each diagram says which: either not in the repo (hosting set up outside it, or not running), or hidden or not implemented.

"Verified against" names the commit whose code each diagram was checked against on 22 October 2026. develop 98ff559 is the current develop; main fe64455 is develop plus PR #4 (499c39d).

Class diagrams use UML notation: `+name: Type = default` for attributes, `+Operation(param: Type) : ReturnType` for operations, `-` private, `#` protected, underlined = static, and a lollipop for an interface the class provides. The design class model is split into eight diagrams so that each one stays readable at A4 portrait width (each is at most about 1,270 px wide at 16 px text).

## Hosting as built

- Database: PostgreSQL only. `Program.cs` calls `UseNpgsql`, the project references only the Npgsql packages, and all five migrations are PostgreSQL-specific. No branch has a SQL Server provider, so the API cannot use Azure SQL as built. The team's hosted database is Supabase PostgreSQL.
- Production configuration is not in the repo: `appsettings.json` falls back to `localhost:5432` (`strategies_db`) and there is no `appsettings.Production.json`. The hosted connection string, JWT key, Gemini key and SMTP settings must be App Service application settings.
- API host: Azure App Service, deployed by the team with `dotnet publish` and a CLI deploy from a developer PC. No publish profile, Azure config or deploy workflow is in the repo, so the diagrams mark it "per team".
- File uploads: the API's local disk (`wwwroot/uploads`). No object storage (the Task 1 plan had Cloudflare R2).
- AI: one agent, Gemini triage, which falls back to `NeedsReview` when the key is missing, the call fails or the reply cannot be parsed. The checklist and summarisation agents exist only as `AgentType` values.
- GitHub Actions: `ci.yml` and `deploy-pages.yml` exist, but the organisation's Actions are suspended (billing). Every run since 21 October fails with "The job was not started", so checks run locally before each PR.

| Diagram | What it shows | Report section | Verified against |
|---|---|---|---|
| [ux-journey-web](ux-journey-web.mmd) | Web portal public routes, sign-in and the redirect into each role workspace | 9.1.7 User Experience Journey Map | develop 98ff559 |
| [ux-journey-web-workspaces](ux-journey-web-workspaces.mmd) | Admin, consultant and client screens, panels and hidden placeholder routes | 9.1.7 User Experience Journey Map | develop aff26b0 |
| [ux-journey-mobile](ux-journey-mobile.mmd) | Android app screens (client only): restore, login, three tabs, sign out | 9.1.7 User Experience Journey Map | develop 98ff559 |
| [domain-class](domain-class.mmd) | Business concepts, key attributes and multiplicities, no methods | 9.1.9 Domain modelling | develop 98ff559 |
| [design-class](design-class.mmd) | Design class 1 of 8: User, Lead, Case, VisaCategory with typed attributes, PK/FK, multiplicities | 9.1.9 Design artifacts | develop 98ff559 |
| [design-class-documents](design-class-documents.mmd) | Design class 2 of 8: Document, DocumentType, CaseStageHistory and their delete rules | 9.1.9 Design artifacts | develop 98ff559 |
| [design-class-supporting](design-class-supporting.mmd) | Design class 3 of 8: Message, VisaRecord, ChecklistTemplate, AgentRun, AuditLog | 9.1.9 Design artifacts | develop 98ff559 |
| [design-class-enums](design-class-enums.mmd) | Design class 4 of 8: the seven enumerations and the properties that use them | 9.1.9 Design artifacts | develop 98ff559 |
| [design-class-controllers-1](design-class-controllers-1.mmd) | Design class 5 of 8: Auth, Leads and Cases controllers, typed operations, access rules | 9.1.9 Design artifacts | develop 98ff559 |
| [design-class-controllers-2](design-class-controllers-2.mmd) | Design class 6 of 8: Documents, Admin, Stats, Notification and DocumentTypes controllers | 9.1.9 Design artifacts | develop 98ff559 |
| [design-class-services-1](design-class-services-1.mmd) | Design class 7 of 8: authentication services, provided interfaces (lollipops), RefreshToken | 9.1.9 Design artifacts | develop 98ff559 |
| [design-class-services-2](design-class-services-2.mmd) | Design class 8 of 8: email, Gemini triage, CaseManagementService, ApplicationDbContext, middleware | 9.1.9 Design artifacts | develop 98ff559 |
| [seq-login](seq-login.mmd) | Sign in: rate limit 429, validation 400, 401, BCrypt check, token issue | 9.1.10 Implementation | develop 98ff559 |
| [seq-token-refresh](seq-token-refresh.mmd) | Silent refresh after a 401, rotation, session expiry | 9.1.10 Implementation | develop 98ff559 |
| [seq-register](seq-register.mmd) | Client registration (API only) and why the new account cannot sign in | 9.1.10 Implementation | develop 98ff559 |
| [seq-password-reset](seq-password-reset.mmd) | Forgot password email and reset, with the known failure points | 9.1.10 Implementation | main fe64455 |
| [seq-document-upload](seq-document-upload.mmd) | Client uploads a new copy: browser checks, file upload, document record | 9.1.10 Implementation | develop 98ff559 |
| [seq-case-stage-change](seq-case-stage-change.mmd) | Staff change a case stage: role and ownership checks, history and audit rows | 9.1.10 Implementation | develop 98ff559 |
| [state-case](state-case.mmd) | Case stages, the unused order-enforcing service and the unused archive flag | 9.1.10 Implementation | develop 98ff559 |
| [state-user-account](state-user-account.mmd) | Unconfirmed, active and inactive accounts (suspend and soft delete share one state) | 9.1.10 Implementation | main fe64455 |
| [state-document](state-document.mmd) | Pending, approved, rejected and archived documents | 9.1.10 Implementation | develop 98ff559 |
| [deployment](deployment.mmd) | Nodes, artefacts, dev and production environments, config sources, CLI publish to App Service | 9.1.10 Implementation (deployment plan) | develop 98ff559 |
| [pattern-dependency-injection](pattern-dependency-injection.mmd) | Object diagram of the instances and lifetimes behind one sign-in request | 9.1.12 Design Patterns | develop 98ff559 |
| [pattern-strategy](pattern-strategy.mmd) | IAgentService (Gemini with fallback to NeedsReview), IEmailService and IRefreshTokenStore | 9.1.12 Design Patterns | develop 98ff559 |
| [pattern-middleware-pipeline](pattern-middleware-pipeline.mmd) | Middleware chain in Program.cs order and where it short-circuits | 9.1.12 Design Patterns | develop 98ff559 |
| [pattern-dto](pattern-dto.mmd) | Request and response DTOs around entities for login and the case list | 9.1.12 Design Patterns | develop 98ff559 |
| [pattern-mvvm-mobile](pattern-mvvm-mobile.mmd) | Android MVVM: Compose screens, ViewModels with StateFlow, ApiClient | 9.1.12 Design Patterns | develop 98ff559 |
| [architecture-layers](architecture-layers.mmd) | Three tiers and API layers as built, including controllers that skip the service layer | 9.1.12 Architecture Patterns | develop 98ff559 |
| [cloud-architecture](cloud-architecture.mmd) | Hosts, boundaries and protocols: Pages, App Service, Supabase PostgreSQL, Gemini, SMTP | 9.1.12 Cloud | develop 98ff559 |
| [pipeline](pipeline.mmd) | feature -> develop -> main, CI jobs, where tests run, Pages deploy, Actions suspended | 9.1.14 GitHub Actions Pipeline | develop 98ff559 |

Not a design pattern here: Repository. Controllers query `ApplicationDbContext` directly, so it has no diagram.

Drawn from develop, so they lack 499c39d (main only): `design-class-controllers-1`, `design-class-services-1` and `pattern-dependency-injection` (no `PasswordResetTokenService` or `PasswordPolicy`), and `seq-register` (no password policy check).

## Where the code differs from the requirements

IDs are from the client requirements list. Checked against `develop`, plus `origin/main` for password reset.

| Area | Requirement | What the code does |
|---|---|---|
| Case stages | FR-4.4 five milestones | Any stage can be set from any stage, including the same stage and archived cases. Only the unused `CaseManagementService.AdvanceCaseStageAsync` enforces the order |
| Case archive | FR-8.2 soft delete | `Case.IsArchived` exists, but no endpoint sets it |
| Registration | FR-1.2 | Accounts start with `EmailConfirmed = false`, no confirmation email or endpoint exists, and sign-in requires it, so a self-registered account cannot sign in. On main the password policy now applies at registration |
| Password reset | FR-1.2 | Works on main: single-use 30-minute token, own audience, `/#/` link, policy, refresh tokens revoked. Not yet on develop. `App:BaseUrl` is set only for Development, so production sends no email until it is configured. A suspended or deleted user can still reset (cannot sign in) |
| Account states | NFR-1.3 | No lockout, only a per-IP limit of 5 requests per minute. Suspend and soft delete are the same stored state |
| User management | FR-3.1 | No create or edit endpoint |
| Checklists, expiry, messages | FR-5.1, FR-7.1, FR-7.2 | ChecklistTemplate, VisaRecord and AgentRun are never used. No endpoint creates a Message. Email only for password reset |
| Architecture | NFR-2.1, NFR-2.2 | ASP.NET Core API, not Node/Express. No `/api/v1` versioning |
| Data protection | NFR-1.1 | Uploads are stored unencrypted on the API's local disk. Refresh tokens are kept in memory |
| Audit | FR-1.4 | Uploads and AI triage write no audit row. No role column |
| Data integrity | none | No unique index on `User.Email` |
| Branching | 9.1.14 flow | PR #4 (499c39d) merged a feature branch straight into main, so main is ahead of develop |

## Re-rendering

How to re-render: from `docs/diagrams`, with Node 22, run `npm i --no-save --no-package-lock @mermaid-js/mermaid-cli@11` once, then `node render.mjs` (all diagrams) or `node render.mjs NAME ...`. It uses the installed Edge (set `BROWSER_PATH` for Chrome) and writes the `.svg` and 3x `.png` with the settings below in one browser session.

The same output one diagram at a time, without the script:

```
npx -y @mermaid-js/mermaid-cli@11 -c mermaid-config.json -b white -w 1600 -i NAME.mmd -o NAME.svg
npx -y @mermaid-js/mermaid-cli@11 -c mermaid-config.json -b white -w 1600 -s 3 -i NAME.mmd -o NAME.png
```

Without a downloaded Chromium, add `-p puppeteer.json` with `{"executablePath": "<path to Chrome or Edge>"}`
