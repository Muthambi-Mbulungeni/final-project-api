import { describe, expect, it } from 'vitest'
import {
  isOwnMessage,
  MESSAGE_MAX_LENGTH,
  mergeMessages,
  messageBody,
  messageError,
  messageLength,
  messagesPath,
  sendErrorOutcome,
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

  it('words a 429 plainly', () => {
    expect(sendErrorOutcome(new ApiError('Too Many Requests', { status: 429 })).message).toMatch(/too quickly/)
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
