// Thin wrapper around fetch for the Strategies API.
// Requests use relative /api paths: Vite forwards them to the API in development.
// Every failure becomes an ApiError with a readable message, using the API's
// standard error shape { message, errors } when the server provides it.
//
// Authentication: every request carries the in-memory access token as a Bearer header.
// When the API answers 401, the token is refreshed once and the request is retried.
// Refresh tokens rotate (each works once), so requests that fail at the same moment
// share a single refresh call instead of each starting their own.

import { endSession, getAccessToken, getStoredRefreshToken, startSession } from './session'

export class ApiError extends Error {
  constructor(message, { status = 0, errors = [] } = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

// Developers get a hint about the API; clients and staff get plain language.
function unreachableMessage() {
  return import.meta.env.DEV
    ? 'The server could not be reached. Check that the API is running and try again.'
    : 'We cannot reach the service right now. Please try again shortly.'
}
const SERVER_ERROR_MESSAGE = 'Something went wrong on our side. Please try again in a moment.'
const REFRESH_PATH = '/api/auth/refresh'

let refreshInFlight = null
let sessionExpiredHandler = () => {}

// The app registers what should happen when a session can no longer be renewed.
export function onSessionExpired(handler) {
  sessionExpiredHandler = typeof handler === 'function' ? handler : () => {}
}

async function readBody(response) {
  const text = await response.text()
  if (!text) return null
  try {
    return JSON.parse(text)
  } catch {
    return text
  }
}

function toApiError(response, body) {
  // 502, 503 and 504 come from the gateway in front of the API: the API itself did not answer.
  if ([502, 503, 504].includes(response.status)) return new ApiError(unreachableMessage(), { status: response.status })
  if (response.status >= 500 && (body === null || typeof body !== 'object')) {
    return new ApiError(SERVER_ERROR_MESSAGE, { status: response.status })
  }

  const message =
    body && typeof body === 'object' && body.message
      ? body.message
      : `The request failed with status ${response.status}.`

  return new ApiError(message, {
    status: response.status,
    errors: Array.isArray(body?.errors) ? body.errors : [],
  })
}

async function send(path, options) {
  try {
    return await fetch(path, options)
  } catch (error) {
    if (error.name === 'AbortError') throw error
    throw new ApiError(unreachableMessage())
  }
}

function isAuthPath(path) {
  return path.startsWith('/api/auth/')
}

async function requestNewTokens() {
  const refreshToken = getStoredRefreshToken()
  const options = { method: 'POST', headers: { Accept: 'application/json' }, credentials: 'same-origin' }

  // Cookie mode (default): the browser sends the httpOnly cookie, so no body is needed.
  // Fallback mode: send the refresh token the API gave us in the body.
  if (refreshToken) {
    options.headers['Content-Type'] = 'application/json'
    options.body = JSON.stringify({ refreshToken })
  }

  let response
  try {
    response = await fetch(REFRESH_PATH, options)
  } catch {
    return false
  }
  if (!response.ok) return false

  const body = await readBody(response)
  return startSession(body) !== null
}

// Asks the API for a new access token. Calls made while one is in progress share it.
// Resolves to true when the session was renewed.
export function refreshSession() {
  if (!refreshInFlight) {
    refreshInFlight = requestNewTokens().finally(() => {
      refreshInFlight = null
    })
  }
  return refreshInFlight
}

// Runs once, however many requests discover the expiry at the same time.
function expireSession() {
  if (!getAccessToken()) return
  endSession()
  sessionExpiredHandler()
}

// fetch with the Bearer header, plus one refresh-and-retry when the access token has expired.
async function authorizedFetch(path, options = {}, retried = false) {
  const sentToken = getAccessToken()
  const headers = { ...(options.headers ?? {}) }
  if (sentToken) headers.Authorization = `Bearer ${sentToken}`

  const response = await send(path, { credentials: 'same-origin', ...options, headers })

  // Only a signed-in request to a normal endpoint is worth refreshing for.
  if (response.status !== 401 || retried || !sentToken || isAuthPath(path)) return response

  // Another request already renewed the token while this one was in flight: just retry.
  const latestToken = getAccessToken()
  if (latestToken && latestToken !== sentToken) return authorizedFetch(path, options, true)

  if (await refreshSession()) return authorizedFetch(path, options, true)

  expireSession()
  return response
}

// Sends a request with an optional JSON body and returns the parsed response body (or null).
async function sendJson(method, path, data, signal) {
  const headers = { Accept: 'application/json' }
  const options = { method, headers, signal }
  if (data !== undefined) {
    headers['Content-Type'] = 'application/json'
    options.body = JSON.stringify(data)
  }

  const response = await authorizedFetch(path, options)
  const body = await readBody(response)
  if (!response.ok) throw toApiError(response, body)
  return body
}

export function apiGet(path, { signal } = {}) {
  return sendJson('GET', path, undefined, signal)
}

export function apiPost(path, data) {
  return sendJson('POST', path, data)
}

export function apiPatch(path, data) {
  return sendJson('PATCH', path, data)
}

// Sends one file as multipart/form-data in the field the API reads ("file").
// Content-Type is deliberately left unset: the browser adds it with the multipart boundary.
// FormData can be sent again, so the refresh-and-retry above still works.
export async function apiUpload(path, file) {
  const body = new FormData()
  body.append('file', file)

  const response = await authorizedFetch(path, { method: 'POST', headers: { Accept: 'application/json' }, body })
  const result = await readBody(response)
  if (!response.ok) throw toApiError(response, result)
  return result
}

// Reads the filename from a Content-Disposition header, preferring the UTF-8 form.
export function filenameFromDisposition(header, fallback) {
  if (!header) return fallback

  const encoded = header.match(/filename\*=UTF-8''([^;]+)/i)
  if (encoded) {
    try {
      return decodeURIComponent(encoded[1].trim())
    } catch {
      // Malformed encoding: fall back to the plain filename below.
    }
  }

  const plain = header.match(/filename="?([^";]+)"?/i)
  return plain ? plain[1].trim() : fallback
}

// Fetches a protected file with the signed-in user's token. Returns { blob, filename }.
export async function apiFetchFile(path, fallbackName = 'download') {
  const response = await authorizedFetch(path)

  if (!response.ok) {
    const body = await readBody(response)
    throw toApiError(response, body)
  }

  const blob = await response.blob()
  const filename = filenameFromDisposition(response.headers.get('Content-Disposition'), fallbackName)
  return { blob, filename }
}

function saveBlob(blob, filename) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 0)
}

// How long an opened file's temporary URL stays valid: long enough for the new tab to load it.
const OPENED_FILE_URL_LIFETIME_MS = 60_000

// Shows a fetched file in a new tab. The tab is opened only after the file arrived, so a
// missing file shows an error here instead of leaving an empty tab. If a popup blocker stops
// the tab, the file is saved instead. The new tab cannot reach back into the portal (opener).
// `browser` is injectable for tests.
export function openBlob(blob, filename, browser = {}) {
  const { open = (url) => window.open(url, '_blank'), save = saveBlob, later = setTimeout } = browser
  const url = URL.createObjectURL(blob)
  const tab = open(url)
  if (!tab) {
    URL.revokeObjectURL(url)
    save(blob, filename)
    return 'saved'
  }
  tab.opener = null
  later(() => URL.revokeObjectURL(url), OPENED_FILE_URL_LIFETIME_MS)
  return 'opened'
}

// Opens a protected file in a new tab (or saves it if tabs are blocked). Returns 'opened' or 'saved'.
export async function apiOpenFile(path, fallbackName = 'file') {
  const { blob, filename } = await apiFetchFile(path, fallbackName)
  return openBlob(blob, filename)
}

// Downloads a file from the API and saves it through the browser.
// Returns the filename used.
export async function apiDownload(path, fallbackName = 'download') {
  const { blob, filename } = await apiFetchFile(path, fallbackName)
  saveBlob(blob, filename)
  return filename
}
