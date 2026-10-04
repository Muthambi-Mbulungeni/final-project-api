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

Without the hosted PostgreSQL, sign-in works but the data tabs show "Something went wrong on our side" (the API returns 500).
That is the expected state until Lekabe's connection string + seed data exist.

## What it does
| Screen | API | Notes |
|---|---|---|
| Sign in | `POST /api/auth/login` | Non-Client roles are refused and their session revoked |
| Silent restore | `POST /api/auth/refresh` | On launch, using the stored refresh token |
| Progress | `GET /api/cases/client/{userId}` | Newest case; reference = first 8 chars of id, upper case; 5-stage vertical stepper + "What happens next" |
| Documents | `GET /api/documents?caseId=` | Read-only list with status (Pending / Approved / Rejected) |
| Messages | `GET /api/notifications`, `PATCH /api/notifications/{id}/read` | Unread badge from `unreadCount` |
| Sign out | `POST /api/auth/revoke` | Local tokens are cleared even if offline |

## How sessions work (`data/ApiClient.kt`)
- Access token: memory only. Refresh token: `EncryptedSharedPreferences` (Android Keystore). Never logged. `allowBackup=false`.
- On a 401 an OkHttp `Authenticator` refreshes **once** and retries **once**. A lock makes concurrent 401s wait for the same refresh,
  because the API rotates refresh tokens (a second refresh with the revoked token would log the user out).
- Refresh returning 401 = session over: tokens cleared, app returns to login with "Your session has expired".
- Error wording is centralised in `ApiException.userMessage()`; raw server text is never shown.

## Cleartext / HTTPS
Cleartext http is allowed only for `10.0.2.2` and `localhost` through `src/debug` (debug builds). Release builds are HTTPS only:
set the real address in `app/build.gradle.kts` (`release` > `API_BASE_URL`).

## Known gaps (waiting on the API)
- Document **upload**: `POST /api/documents` only records metadata (FileName/FilePath); there is no file-upload endpoint yet.
- Registration, email confirmation and password reset: endpoints don't exist yet.
- Font is the system default (web uses IBM Plex Sans). Add the font files under `res/font` to match exactly.
- No automated tests yet; manual checks against the dev API are listed above.
