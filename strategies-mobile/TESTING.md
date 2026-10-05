# Mobile testing log (Nexus Client, Android)

Status key: PASS = checked and working, TODO = not yet checked.

## 1. Manual end-to-end test against the real API + database
Tester: friend's machine (Android emulator, real API and hosted DB). Build: BUILD SUCCESSFUL.
Screenshots: to be attached (kept by the tester).

| # | Feature | Expected | Actual | Result |
|---|---|---|---|---|
| 1 | Sign in as client | Opens the app on the Progress tab | Signed in and loaded the case | PASS |
| 2 | Progress stepper | Shows the case's current stage, same as the web portal | Showed stage 3, matching the web | PASS |
| 3 | Documents | Lists the case's documents with status and reviewer note | Documents shown, including a rejection note | PASS |
| 4 | Messages | Lists notifications, unread badge from `unreadCount` | Messages shown, 2 unread | PASS |
| 5 | Wrong password | One generic message, no hint which field was wrong | TODO | TODO |
| 6 | Staff account sign-in | Refused: "This app is for clients. Staff, please use the web portal." | TODO | TODO |
| 7 | 6th login in a minute | "Too many attempts, wait a minute and try again." | TODO | TODO |
| 8 | Reopen app | Silent restore, no login screen | TODO | TODO |
| 9 | Expired access token | One refresh, request retried, user stays signed in | TODO | TODO |
| 10 | Sign out | Returns to login; refresh token revoked and cleared | TODO | TODO |
| 11 | API stopped | "We can't reach the service right now..." (no raw errors) | TODO | TODO |
| 12 | Tap an unread message | Dot clears, badge count drops by one | TODO | TODO |

### Re-run after the fixes (5 Oct 2026)
Tester: Riaad. Branch `fix/riaad-mobile-signin`, debug build on an Android Studio emulator (Medium Phone, API 37.1 image),
real `Strategies.Api` on the same PC and a local PostgreSQL with the demo seed (3 migrations, 11 users).
API calls checked in Android Studio's Network Inspector. Screenshots: [README](../README.md#android-app-client-role).

| # | Actual | Result |
|---|---|---|
| 1 | Signed in as client@nexus.com, opened on My case | PASS |
| 2 | Stage 3 of 5, Under review, same as the web portal | PASS |
| 3 | Medical report "Needs replacing" with the consultant's note; 1 of 3 approved | PASS |
| 4 | 2 messages, 2 unread, case 6ADCBE68 | PASS |
| 5 | Only "Incorrect email or password." shows (the earlier "You have been signed out." notice is cleared) | PASS |
| 6 | admin@nexus.com refused with "This app is for clients. Staff, please use the web portal."; Network Inspector: `login` 200, then `revoke` 200 | PASS |
| 10 | Returned to sign-in with "You have been signed out." (the revoke call on sign-out was not captured) | PASS |
| 12 | Dot cleared, badge 2 to 1; still 1 unread after signing in again (saved on the server) | PASS |
| 7, 8, 9, 11 | Not run | TODO |

## 2. Automated unit tests
`app/src/test/java/com/nexus/strategies/data/ModelsTest.kt` covers stage parsing and wording, case reference and
placeholder hiding, notification shapes (real API and old mock), date formatting, the shared error messages, and
which documents the client sees (a rejected copy is hidden once a newer copy of the same type exists, as on the web).
Run in Android Studio: right-click `ModelsTest` > Run, or `./gradlew testDebugUnitTest`.
Result (5 Oct 2026, `./gradlew testDebugUnitTest`): 19 of 19 passed, 0 failures. With the hidden-copy rule switched
off on purpose, the 2 tests for it failed, so they test the rule.

## 3. Known issues
- Registration, password reset and file upload are not in the app. Clients upload new copies on the web portal;
  password reset waits for the API fix in [ISS-01](../docs/ISSUES.md#iss-01).
- Release builds still point at `https://CHANGE-ME.example.com/` until the API is hosted.
- Notification scoping to the user's own cases (POPIA) is done in the API on the version tested; re-check after merges.
