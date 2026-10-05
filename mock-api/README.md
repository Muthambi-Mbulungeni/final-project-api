# Mock API (testing only)

A tiny stand-in for `Strategies.Api` so the Android app can be tested when the hosted database isn't available.
It listens on the same address (port 5151) and returns the same response shapes, so the app needs no changes.
Fake tokens and fake data: **dev use only, never deploy it.**

## Run
1. Install Node.js 18 or newer (check with `node --version`).
2. **Stop the real API first** (only one thing can use port 5151).
3. In this folder: `node server.js` and wait for "Mock API listening on http://localhost:5151".
4. Run the app on the emulator and sign in with `client@nexus.com` / `ClientPassword123!`.

## Demo data
- One active case (Critical Skills Work Visa, stage 3 of 5, "Under review") plus an older archived case (must be ignored).
- Documents: Medical report (Needs replacing, with a consultant note), Police clearance (Awaiting review), Passport (Approved).
- Messages: 2 unread, 1 read.
- `admin@nexus.com` / `AdminPassword123!` signs in as Admin, so the app must refuse it ("This app is for clients...").

## Test controls (open in a browser on your PC)
| Address | Effect | Tests |
|---|---|---|
| `http://localhost:5151/__mock/expire` | Kills all access tokens | Silent refresh: pull to refresh and the app should carry on without asking you to sign in |
| `http://localhost:5151/__mock/fail?code=500` | Data calls return 500 | "Something went wrong on our side" banner |
| `http://localhost:5151/__mock/fail?code=503` | Data calls return 503 | "We can't reach the service right now" banner |
| `http://localhost:5151/__mock/fail?code=off` | Back to normal | |

To test with short-lived tokens, start it with `set TOKEN_SECONDS=20 && node server.js` (Command Prompt).
The window prints one line per request (no tokens), which makes good test evidence.
Refresh tokens rotate and are single-use, like the real API.
