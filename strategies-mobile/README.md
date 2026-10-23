# Nexus Client (Android) - Team Nexus Solutions

Kotlin + Jetpack Compose client app for the Strategies Migration Platform. **Clients only**: staff use the web portal.
It talks to the same `Strategies.Api` as the web (see Riaad's context file) and follows the same wording, stage names and colours.

## Open and run
1. Recent Android Studio. **File > Open** this `strategies-mobile` folder and let Gradle sync. It uses Gradle 9.1.0 (see
   `gradle/wrapper/gradle-wrapper.properties`), which runs on any JDK from 17 to 25, so the Gradle JDK setting can stay on its default.
   If Studio offers to install Android SDK Platform 36, accept. Don't add `org.gradle.java.home` to `gradle.properties` (it breaks other machines).
   If Studio suggests upgrading the Android Gradle Plugin, you can ignore it.
2. Start the API first: in `Strategies.Api` run `dotnet run --launch-profile http` and wait for `Now listening on http://localhost:5151`
   (needs `dotnet user-secrets set "Jwt:Key" "<long random string>"`).
3. Run the `app` configuration on an emulator. Debug builds call `http://10.0.2.2:5151/` (the emulator's name for your PC).
4. Sign in with the dev client account: `client@nexus.com` / `ClientPassword123!`
   (login is rate limited to 5 per minute; the 6th shows "Too many attempts").

The API needs a database with the migrations and the demo seed ([database/seed/README.md](../database/seed/README.md));
without it, sign-in works but the data tabs show "Something went wrong on our side" (the API returns 500).
Only the real API is used for the results in [TESTING.md](TESTING.md); `mock-api/` is for development only.

## What it does
| Screen | API | Notes |
|---|---|---|
| Sign in | `POST /api/auth/login` | Non-Client roles are refused and their session revoked |
| Silent restore | `POST /api/auth/refresh` | On launch, using the stored refresh token |
| Progress | `GET /api/cases/client/{userId}` | Newest case; reference = first 8 chars of id, upper case; 5-stage vertical stepper + "What happens next" |
| Documents | `GET /api/documents?caseId=` | Read-only list with status (Pending / Approved / Rejected); a rejected copy is hidden once a newer copy of the same type exists, as on the web |
| Send a document | `GET /api/DocumentTypes`, `POST /api/Documents/upload`, then `POST /api/Documents` | Take a photo (camera app) or choose one (gallery picker), no permissions needed. Shrunk to a JPEG under 4.5 MB (API limit is 5 MB), rotated upright, previewed, then sent as the chosen document type; it shows as "Awaiting review". See `data/PhotoPrep.kt` |
| Messages | `GET /api/notifications`, `PATCH /api/notifications/{id}/read` | Unread badge from `unreadCount` |
| Sign out | `POST /api/auth/revoke` | Local tokens are cleared even if offline |

## Look and feel (matches the web portal)
- Logo (`res/drawable-nodpi/strategies_logo.png`, copied from `strategies-web/src/assets`), brand red `#c4161c`, warm stone greys,
  red uppercase "eyebrow" labels, bordered white cards, the red-tick stepper with "Current stage", and document status badges.
- Header like the web: logo, bell with unread badge (opens Messages), and the user's initials (tap for Sign out).
- Wording and the "06 Sept 2026" date style follow the web. Document statuses are shown as Awaiting review / Approved / Needs replacing.
- **Font (IBM Plex Sans):** download IBM Plex Sans from Google Fonts (static files) and add these four files, with exactly these names,
  to a new folder `app/src/main/res/font/`: `ibm_plex_sans_regular.ttf`, `ibm_plex_sans_medium.ttf`,
  `ibm_plex_sans_semibold.ttf`, `ibm_plex_sans_bold.ttf`. The app picks them up automatically; without them it uses the system font.
- Not in the app on purpose: "Forgot password?", "Start an enquiry" and the privacy page (they live on the web).

## How sessions work (`data/ApiClient.kt`)
- Access token: memory only. Refresh token: `EncryptedSharedPreferences` (Android Keystore). Never logged. `allowBackup=false`.
- On a 401 an OkHttp `Authenticator` refreshes **once** and retries **once**. A lock makes concurrent 401s wait for the same refresh,
  because the API rotates refresh tokens (a second refresh with the revoked token would log the user out).
- Refresh returning 401 = session over: tokens cleared, app returns to login with "Your session has expired".
- Error wording is centralised in `ApiException.userMessage()`; raw server text is never shown.

## Cleartext / HTTPS
Cleartext http is allowed only for `10.0.2.2` and `localhost` through `src/debug` (debug builds). Release builds are HTTPS only:
set the real address in `app/build.gradle.kts` (`release` > `API_BASE_URL`).

## Tests
`./gradlew testDebugUnitTest` runs the unit tests in `ModelsTest` (19, all passing on 5 Oct 2026). Manual checks against the
real API are in [TESTING.md](TESTING.md).

## Known gaps
- Messages are read-only. Sending a message needs an API endpoint that does not exist yet (the API only has
  `GET /api/notifications` and `PATCH /api/notifications/{id}/read`).
- Registration and password reset are not in the app. Password reset waits for the API fix in
  [ISS-01](../docs/ISSUES.md#iss-01).
