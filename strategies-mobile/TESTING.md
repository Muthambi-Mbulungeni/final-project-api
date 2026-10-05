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

## 2. Automated unit tests
`app/src/test/java/com/nexus/strategies/data/ModelsTest.kt` covers stage parsing and wording, case reference and
placeholder hiding, notification shapes (real API and old mock), date formatting and the shared error messages.
Run in Android Studio: right-click `ModelsTest` > Run, or `./gradlew testDebugUnitTest`.
Result: TODO (record pass count and screenshot here).

## 3. Known issues
- Registration, password reset and file upload are not in the app because the API endpoints don't exist yet.
- Notification scoping to the user's own cases (POPIA) is done in the API on the version tested; re-check after merges.
