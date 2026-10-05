// Stand-in for Strategies.Api, for testing the Android app when the real database isn't available.
// Same address (port 5151), same endpoints and response shapes the app uses, same dev accounts.
// No packages needed: just Node.js 18+.   Run:  node server.js
// DEV USE ONLY. Fake tokens, fake data, no security. Never deploy this.

const http = require('http');
const crypto = require('crypto');

const PORT = 5151;
// TOKEN_SECONDS=20 node server.js  ->  access tokens die after 20s, so you can watch the app refresh silently.
const ACCESS_SECONDS = Number(process.env.TOKEN_SECONDS || 3600);

const users = {
  'client@nexus.com':     { pw: 'ClientPassword123!',     id: '77777777-7777-7777-7777-777777777777', role: 'Client',     name: 'John Doe' },
  'consultant@nexus.com': { pw: 'ConsultantPassword123!', id: '33333333-3333-3333-3333-333333333334', role: 'Consultant', name: 'Consultant User' },
  'admin@nexus.com':      { pw: 'AdminPassword123!',      id: '00000000-0000-0000-0000-000000000001', role: 'Admin',      name: 'System Admin' },
};

const day = 86400000;
const ago = (days) => new Date(Date.now() - days * day).toISOString().slice(0, 19);
const CASE_ID = '6adcbe68-1111-4222-8333-444455556666';

const cases = [
  // Newest case is the one the app shows (stage 3 of 5, same as the web demo).
  { id: CASE_ID, clientId: users['client@nexus.com'].id, clientName: 'John Doe', consultantName: 'Consultant User',
    visaCategoryName: 'Critical Skills Work Visa', stage: 'UnderReview', isArchived: false,
    createdAt: ago(24), lastUpdated: ago(1) },
  // An older, archived case: proves the app picks the newest/active one.
  { id: 'a1b2c3d4-0000-4000-8000-000000000001', clientId: users['client@nexus.com'].id, clientName: 'John Doe',
    consultantName: 'Unassigned', visaCategoryName: 'N/A', stage: 'FinalDecision', isArchived: true,
    createdAt: ago(400), lastUpdated: ago(300) },
];

const documents = [
  { id: 'd0000000-0000-4000-8000-000000000001', caseId: CASE_ID, documentTypeName: 'Medical report', fileName: 'medical-report-john-doe.pdf',
    status: 'Rejected', reviewComment: 'Report is older than six months. Please upload a recent one.', uploadedAt: ago(11) },
  { id: 'd0000000-0000-4000-8000-000000000002', caseId: CASE_ID, documentTypeName: 'Police clearance certificate', fileName: 'police-clearance-john-doe.pdf',
    status: 'Pending', reviewComment: null, uploadedAt: ago(1) },
  { id: 'd0000000-0000-4000-8000-000000000003', caseId: CASE_ID, documentTypeName: 'Passport', fileName: 'passport-john-doe.pdf',
    status: 'Approved', reviewComment: 'Document reviewed and accepted.', uploadedAt: ago(26) },
];

const notifications = [
  { id: 'e0000000-0000-4000-8000-000000000001', caseId: CASE_ID, body: 'Your medical report needs to be replaced. Please see the Documents tab.', isRead: false, sentAt: ago(1) },
  { id: 'e0000000-0000-4000-8000-000000000002', caseId: CASE_ID, body: 'We have started reviewing your complete file.', isRead: false, sentAt: ago(3) },
  { id: 'e0000000-0000-4000-8000-000000000003', caseId: CASE_ID, body: 'Welcome. Please upload the documents on your checklist.', isRead: true, sentAt: ago(20) },
];

const accessTokens = new Map();   // token -> { userId, expires }
const refreshTokens = new Map();  // token -> userId (single use: rotated on every refresh)
const loginAttempts = [];         // timestamps, for the 5-per-minute limit
let failCode = null;              // set via /__mock/fail?code=500

const newToken = () => crypto.randomBytes(24).toString('hex');
const byId = (id) => Object.values(users).find((u) => u.id === id);

function issue(user) {
  const accessToken = newToken();
  const refreshToken = newToken();
  accessTokens.set(accessToken, { userId: user.id, expires: Date.now() + ACCESS_SECONDS * 1000 });
  refreshTokens.set(refreshToken, user.id);
  return {
    accessToken,
    tokenExpiry: new Date(Date.now() + ACCESS_SECONDS * 1000).toISOString(),
    refreshToken,
    role: user.role,
    displayName: user.name,
    userId: user.id,
  };
}

function send(res, status, body) {
  res.writeHead(status, { 'Content-Type': 'application/json' });
  res.end(body === undefined ? '' : JSON.stringify(body));
}

function readJson(req) {
  return new Promise((resolve) => {
    let raw = '';
    req.on('data', (c) => (raw += c));
    req.on('end', () => { try { resolve(JSON.parse(raw || '{}')); } catch { resolve({}); } });
  });
}

function authedUser(req) {
  const h = req.headers.authorization || '';
  const t = accessTokens.get(h.replace(/^Bearer\s+/i, ''));
  if (!t || t.expires < Date.now()) return null;
  return byId(t.userId);
}

const paged = (data) => ({ data, totalRecords: data.length, totalPages: 1, currentPage: 1, pageSize: data.length });

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, 'http://localhost');
  const path = url.pathname;
  const log = (status) => console.log(`${new Date().toLocaleTimeString()}  ${req.method} ${path} -> ${status}`);
  const reply = (status, body) => { log(status); send(res, status, body); };

  // ---- Test controls (open these in a browser on your PC) ----
  if (path === '/__mock/fail') {
    const c = url.searchParams.get('code');
    failCode = c && c !== 'off' ? Number(c) : null;
    return reply(200, { failing: failCode });
  }
  if (path === '/__mock/expire') { accessTokens.clear(); return reply(200, { expired: 'all access tokens' }); }

  // ---- Auth ----
  if (req.method === 'POST' && path === '/api/auth/login') {
    const now = Date.now();
    while (loginAttempts.length && now - loginAttempts[0] > 60000) loginAttempts.shift();
    if (loginAttempts.length >= 5) return reply(429, { message: 'Too many requests.' });
    loginAttempts.push(now);
    const { email = '', password = '' } = await readJson(req);
    const u = users[String(email).trim().toLowerCase()];
    if (!u || u.pw !== password) return reply(401, { message: 'Invalid email or password.' });
    return reply(200, issue(u));
  }
  if (req.method === 'POST' && path === '/api/auth/refresh') {
    const { refreshToken = '' } = await readJson(req);
    const userId = refreshTokens.get(refreshToken);
    if (!userId) return reply(401, { message: 'Invalid refresh token.' });
    refreshTokens.delete(refreshToken); // rotate: the old one is dead immediately
    return reply(200, issue(byId(userId)));
  }
  if (req.method === 'POST' && path === '/api/auth/revoke') {
    const { refreshToken = '' } = await readJson(req);
    refreshTokens.delete(refreshToken);
    return reply(200, { message: 'Token revoked.' });
  }

  // ---- Everything below needs a valid Bearer token ----
  const user = authedUser(req);
  if (!user) return reply(401, { message: 'Unauthorized.' });
  if (failCode) return reply(failCode, { message: 'Simulated failure.' });

  let m;
  if (req.method === 'GET' && (m = path.match(/^\/api\/cases\/client\/([^/]+)$/))) {
    if (m[1] !== user.id) return reply(403, { message: 'Forbidden.' });
    return reply(200, paged(cases.filter((c) => c.clientId === user.id)));
  }
  if (req.method === 'GET' && path === '/api/documents') {
    const caseId = url.searchParams.get('caseId');
    if (!caseId) return reply(400, { message: 'caseId is required.' });
    return reply(200, paged(documents.filter((d) => d.caseId === caseId)));
  }
  if (req.method === 'GET' && path === '/api/notifications') {
    const list = [...notifications].sort((a, b) => b.sentAt.localeCompare(a.sentAt));
    return reply(200, { unreadCount: list.filter((n) => !n.isRead).length, ...paged(list) });
  }
  if (req.method === 'PATCH' && (m = path.match(/^\/api\/notifications\/([^/]+)\/read$/))) {
    const n = notifications.find((x) => x.id === m[1]);
    if (!n) return reply(404, { message: 'Not found.' });
    const was = n.isRead;
    n.isRead = true;
    return reply(200, { message: was ? 'Already marked as read.' : 'Notification marked as read.' });
  }

  reply(404, { message: 'Not found.' });
});

server.on('error', (e) => {
  if (e.code === 'EADDRINUSE') console.error(`Port ${PORT} is busy. Stop the real API (or any other server on ${PORT}) first.`);
  else console.error(e.message);
  process.exit(1);
});

// 127.0.0.1 is what the Android emulator reaches as 10.0.2.2, and avoids a Windows firewall prompt.
server.listen(PORT, '127.0.0.1', () => {
  console.log(`Mock API listening on http://localhost:${PORT}  (access tokens last ${ACCESS_SECONDS}s)`);
  console.log('Accounts: client@nexus.com / ClientPassword123!   admin@nexus.com / AdminPassword123!');
  console.log('Test controls: /__mock/expire   /__mock/fail?code=500   /__mock/fail?code=off');
});
