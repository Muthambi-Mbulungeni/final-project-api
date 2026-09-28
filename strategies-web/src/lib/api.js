// Thin wrapper around fetch for the Strategies API.
// Requests use relative /api paths: Vite forwards them to http://localhost:5151 in development.
// Every failure becomes an ApiError with a readable message, using the API's
// standard error shape { message, errors } when the server provides it.

export class ApiError extends Error {
  constructor(message, { status = 0, errors = [] } = {}) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.errors = errors
  }
}

const UNREACHABLE_MESSAGE = 'The server could not be reached. Check that the API is running and try again.'

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
  if (response.status >= 500 && (body === null || typeof body !== 'object')) {
    return new ApiError(UNREACHABLE_MESSAGE, { status: response.status })
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
    throw new ApiError(UNREACHABLE_MESSAGE)
  }
}

export async function apiGet(path, { signal } = {}) {
  const response = await send(path, { headers: { Accept: 'application/json' }, signal })
  const body = await readBody(response)
  if (!response.ok) throw toApiError(response, body)
  return body
}

// Sends a PATCH request, with an optional JSON body. Returns the parsed response body (or null).
export async function apiPatch(path, data) {
  const options = { method: 'PATCH', headers: { Accept: 'application/json' } }
  if (data !== undefined) {
    options.headers['Content-Type'] = 'application/json'
    options.body = JSON.stringify(data)
  }

  const response = await send(path, options)
  const body = await readBody(response)
  if (!response.ok) throw toApiError(response, body)
  return body
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

// Downloads a file from the API and saves it through the browser.
// Returns the filename used.
export async function apiDownload(path, fallbackName = 'download') {
  const response = await send(path)

  if (!response.ok) {
    const body = await readBody(response)
    throw toApiError(response, body)
  }

  const blob = await response.blob()
  const filename = filenameFromDisposition(response.headers.get('Content-Disposition'), fallbackName)
  const url = URL.createObjectURL(blob)

  const link = document.createElement('a')
  link.href = url
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 0)

  return filename
}
