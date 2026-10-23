import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  canTakePart,
  isNewIncoming,
  isOwnMessage,
  loadNewerMessages,
  markThreadRead,
  MESSAGE_MAX_LENGTH,
  mergeMessages,
  messageBody,
  messageError,
  messageLength,
  messagesPath,
  sendErrorOutcome,
  threadErrorMessage,
} from './messages'
import { ApiError } from './api'

describe('messagesPath', () => {
  it('asks for the first page of 50 by default', () => {
    expect(messagesPath('abc')).toBe('/api/cases/abc/messages?page=1&pageSize=50')
  })

  it('encodes the case id and takes a page', () => {
    expect(messagesPath('a/b', { page: 3 })).toBe('/api/cases/a%2Fb/messages?page=3&pageSize=50')
  })
})

describe('messageLength', () => {
  it('counts code points, so an emoji is one character', () => {
    expect(messageLength('hi 👋')).toBe(4)
    expect('hi 👋'.length).toBe(5)
  })

  it('treats a missing value as empty', () => {
    expect(messageLength(undefined)).toBe(0)
  })
})

describe('messageError', () => {
  it('rejects an empty or whitespace-only message', () => {
    expect(messageError('')).toBe('Write a message before sending.')
    expect(messageError('  \n\t ')).toBe('Write a message before sending.')
    expect(messageError(undefined)).toBe('Write a message before sending.')
  })

  it('allows exactly the limit in code points, even when that is more UTF-16 units', () => {
    expect(messageError('😀'.repeat(MESSAGE_MAX_LENGTH))).toBeUndefined()
  })

  it('rejects one code point over the limit', () => {
    expect(messageError('a'.repeat(MESSAGE_MAX_LENGTH + 1))).toMatch(/^Messages can be up to 2\s000 characters\./)
  })

  it('measures the trimmed text, which is what is stored', () => {
    expect(messageError(`  ${'a'.repeat(MESSAGE_MAX_LENGTH)}  `)).toBeUndefined()
  })
})

describe('messageBody', () => {
  it('sends the trimmed text only', () => {
    expect(messageBody('  Hello there \n')).toEqual({ body: 'Hello there' })
  })
})

describe('sendErrorOutcome', () => {
  it('shows field errors from a 400 under the box', () => {
    const error = new ApiError('Validation failed.', { status: 400, errors: ['Body is too long.', ''] })
    expect(sendErrorOutcome(error)).toEqual({ kind: 'field', errors: ['Body is too long.'] })
  })

  it('switches the box off for a 400 without field errors', () => {
    const error = new ApiError('This case is archived.', { status: 400 })
    expect(sendErrorOutcome(error)).toEqual({ kind: 'closed', message: 'This case is archived.' })
  })

  it('keeps the API message for 403, 404 and unreachable errors', () => {
    expect(sendErrorOutcome(new ApiError('Case not found.', { status: 404 }))).toEqual({
      kind: 'form',
      message: 'Case not found.',
    })
    expect(sendErrorOutcome(new ApiError('', { status: 0 })).message).toBe('Your message was not sent. Please try again.')
  })
})

describe('mergeMessages', () => {
  it('appends new messages and skips ones already shown', () => {
    const existing = [{ id: 1 }, { id: 2 }]
    expect(mergeMessages(existing, [{ id: 2 }, { id: 3 }])).toEqual([{ id: 1 }, { id: 2 }, { id: 3 }])
  })

  it('returns the same array when nothing is new', () => {
    const existing = [{ id: 1 }]
    expect(mergeMessages(existing, [{ id: 1 }])).toBe(existing)
  })
})

describe('isOwnMessage', () => {
  it('compares the sender with the signed-in user as strings', () => {
    expect(isOwnMessage({ senderId: 'u1' }, 'u1')).toBe(true)
    expect(isOwnMessage({ senderId: 'u2' }, 'u1')).toBe(false)
    expect(isOwnMessage({ senderId: undefined }, undefined)).toBe(false)
  })
})

describe('mergeMessages order', () => {
  it('keeps the thread oldest first when a reply sent earlier arrives after my own message', () => {
    const existing = [
      { id: 'a', sentAt: '2026-10-20T08:00:00Z' },
      { id: 'mine', sentAt: '2026-10-20T08:05:00Z' },
    ]
    const merged = mergeMessages(existing, [{ id: 'reply', sentAt: '2026-10-20T08:03:00Z' }])
    expect(merged.map((message) => message.id)).toEqual(['a', 'reply', 'mine'])
  })

  it('keeps the API order for messages sent at the same moment', () => {
    const at = '2026-10-20T08:00:00Z'
    const merged = mergeMessages([{ id: 'z', sentAt: at }], [{ id: 'b', sentAt: at }, { id: 'a', sentAt: at }])
    expect(merged.map((message) => message.id)).toEqual(['z', 'b', 'a'])
  })
})

describe('canTakePart', () => {
  it('lets the client and the consultant send and mark read, not admins', () => {
    expect(canTakePart('client')).toBe(true)
    expect(canTakePart('consultant')).toBe(true)
    expect(canTakePart('admin')).toBe(false)
    expect(canTakePart(undefined)).toBe(false)
  })
})

describe('isNewIncoming', () => {
  it('is a message from someone else that was unread when loaded', () => {
    expect(isNewIncoming({ senderId: 'other', isRead: false }, 'me')).toBe(true)
    expect(isNewIncoming({ senderId: 'other', isRead: true }, 'me')).toBe(false)
    expect(isNewIncoming({ senderId: 'me', isRead: false }, 'me')).toBe(false)
  })
})

describe('threadErrorMessage and sendErrorOutcome for 403', () => {
  it('words a bare 403 plainly', () => {
    const forbidden = new ApiError('The request failed with status 403.', { status: 403 })
    expect(threadErrorMessage(forbidden)).toBe('You no longer have access to messages on this case.')
    expect(sendErrorOutcome(forbidden)).toEqual({ kind: 'closed', message: 'You no longer have access to messages on this case.' })
  })

  it('keeps the API message otherwise', () => {
    expect(threadErrorMessage(new ApiError('Case not found.', { status: 404 }))).toBe('Case not found.')
  })
})

function respond(body, status = 200) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } })
}

describe('loadNewerMessages', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('loads the page that was newest again plus every page after it', async () => {
    const pages = {
      2: { totalPages: 3, data: [{ id: 'm50' }, { id: 'm51' }] },
      3: { totalPages: 3, data: [{ id: 'm100' }] },
    }
    const fetchMock = vi.fn(async (url) => respond(pages[new URL(url, 'http://test').searchParams.get('page')]))
    vi.stubGlobal('fetch', fetchMock)

    const result = await loadNewerMessages('c1', 2)

    expect(result).toEqual({ messages: [{ id: 'm50' }, { id: 'm51' }, { id: 'm100' }], page: 3 })
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})

describe('markThreadRead', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('sends PATCH .../messages/read with no body and returns how many changed', async () => {
    const fetchMock = vi.fn(async () => respond({ message: 'Messages marked as read.', updated: 2 }))
    vi.stubGlobal('fetch', fetchMock)

    expect(await markThreadRead('a/b')).toBe(2)
    const [url, options] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/cases/a%2Fb/messages/read')
    expect(options.method).toBe('PATCH')
    expect(options.body).toBeUndefined()
  })
})
