# Report diagrams

Mermaid sources (`.mmd`) drawn from the code on `develop` (98ff559), each rendered to `.svg` and to a 3x `.png` on a white background for the Word/PDF report. Section numbers refer to the INSY7315 module manual, Task 1 documentation (9.1).

Dashed boxes mean one of two things, and each diagram says which: either assumed (not found in the repo's config), or hidden or not implemented.

| Diagram | What it shows | Report section |
|---|---|---|
| [ux-journey-web](ux-journey-web.mmd) | Web portal public routes, sign-in and the redirect into each role workspace | 9.1.7 User Experience Journey Map |
| [ux-journey-web-workspaces](ux-journey-web-workspaces.mmd) | Admin, consultant and client screens, panels and hidden placeholder routes | 9.1.7 User Experience Journey Map |
| [ux-journey-mobile](ux-journey-mobile.mmd) | Android app screens (client only): restore, login, three tabs, sign out | 9.1.7 User Experience Journey Map |
| [domain-class](domain-class.mmd) | Business concepts, key attributes and multiplicities, no methods | 9.1.9 Domain modelling |
| [design-class](design-class.mmd) | Core entities with types, PK/FK and delete behaviour | 9.1.9 Design artifacts |
| [design-class-supporting](design-class-supporting.mmd) | Supporting entities (Message, VisaRecord, ChecklistTemplate, AgentRun, AuditLog) | 9.1.9 Design artifacts |
| [design-class-enums](design-class-enums.mmd) | The seven enumerations and the entities that use them | 9.1.9 Design artifacts |
| [design-class-controllers](design-class-controllers.mmd) | API controllers with fields, methods, access specifiers and dependencies | 9.1.9 Design artifacts |
| [design-class-services](design-class-services.mmd) | Service interfaces, implementations, refresh token store and DbContext | 9.1.9 Design artifacts |
| [seq-login](seq-login.mmd) | Sign in: rate limit 429, validation 400, 401, BCrypt check, token issue | 9.1.10 Implementation |
| [seq-token-refresh](seq-token-refresh.mmd) | Silent refresh after a 401, rotation, session expiry | 9.1.10 Implementation |
| [seq-register](seq-register.mmd) | Client registration (API only) and why the new account cannot sign in | 9.1.10 Implementation |
| [seq-password-reset](seq-password-reset.mmd) | Forgot password email and reset, with the known failure points | 9.1.10 Implementation |
| [seq-document-upload](seq-document-upload.mmd) | Client uploads a new copy: browser checks, file upload, document record | 9.1.10 Implementation |
| [seq-case-stage-change](seq-case-stage-change.mmd) | Staff change a case stage: role and ownership checks, history and audit rows | 9.1.10 Implementation |
| [state-case](state-case.mmd) | Case stages, the unused order-enforcing service and the unused archive flag | 9.1.10 Implementation |
| [state-user-account](state-user-account.mmd) | Unconfirmed, active and inactive accounts (suspend and soft delete share one state) | 9.1.10 Implementation |
| [state-document](state-document.mmd) | Pending, approved, rejected and archived documents | 9.1.10 Implementation |
| [deployment](deployment.mmd) | Nodes, artefacts, dev and production environments, config sources | 9.1.10 Implementation (deployment plan) |
| [pattern-dependency-injection](pattern-dependency-injection.mmd) | Object diagram of the instances and lifetimes behind one sign-in request | 9.1.12 Design Patterns |
| [pattern-strategy](pattern-strategy.mmd) | IAgentService (Gemini with fallback), IEmailService and IRefreshTokenStore | 9.1.12 Design Patterns |
| [pattern-middleware-pipeline](pattern-middleware-pipeline.mmd) | Middleware chain in Program.cs order and where it short-circuits | 9.1.12 Design Patterns |
| [pattern-dto](pattern-dto.mmd) | Request and response DTOs around entities for login and the case list | 9.1.12 Design Patterns |
| [pattern-mvvm-mobile](pattern-mvvm-mobile.mmd) | Android MVVM: Compose screens, ViewModels with StateFlow, ApiClient | 9.1.12 Design Patterns |
| [architecture-layers](architecture-layers.mmd) | Three tiers and API layers as built, including controllers that skip the service layer | 9.1.12 Architecture Patterns |
| [cloud-architecture](cloud-architecture.mmd) | Hosts, public boundaries and protocols; the API host is assumed | 9.1.12 Cloud |
| [pipeline](pipeline.mmd) | feature -> develop -> main, CI jobs, where tests run, Pages deploy | 9.1.14 GitHub Actions Pipeline |

Not a design pattern here: Repository. Controllers query `ApplicationDbContext` directly, so it has no diagram.

## Re-rendering

From `docs/diagrams`, with Node 22:

```
npx -y @mermaid-js/mermaid-cli@11 -c mermaid-config.json -b white -w 1600 -i NAME.mmd -o NAME.svg
npx -y @mermaid-js/mermaid-cli@11 -c mermaid-config.json -b white -w 1600 -s 3 -i NAME.mmd -o NAME.png
```

Without a downloaded Chromium, add `-p puppeteer.json` with `{"executablePath": "<path to Chrome or Edge>"}`
