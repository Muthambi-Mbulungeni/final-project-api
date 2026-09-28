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

export async function apiGet(path, { signal } = {}) {
  let response

  try {
    response = await fetch(path, { headers: { Accept: 'application/json' }, signal })
  } catch (error) {
    if (error.name === 'AbortError') throw error
    throw new ApiError(UNREACHABLE_MESSAGE)
  }

  const body = await readBody(response)

  if (!response.ok) {
    if (response.status >= 500 && (body === null || typeof body !== 'object')) {
      throw new ApiError(UNREACHABLE_MESSAGE, { status: response.status })
    }

    const message =
      body && typeof body === 'object' && body.message
        ? body.message
        : `The request failed with status ${response.status}.`

    throw new ApiError(message, {
      status: response.status,
      errors: Array.isArray(body?.errors) ? body.errors : [],
    })
  }

  return body
}
