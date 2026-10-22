// @vitest-environment jsdom
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { deferred, json } from '../../test/dom'
import ToastProvider from '../../components/ui/ToastProvider'
import { MESSAGES_READ_EVENT } from '../../lib/messages'
import { endSession, startSession } from '../../lib/session'
import CaseMessages from './CaseMessages'

const CASE_ID = 'case-1'
const ME = 'consultant-1'
const READ_PATH = `/api/cases/${CASE_ID}/messages/read`

function message(id, overrides = {}) {
  return {
    id,
    caseId: CASE_ID,
    senderId: 'client-1',
    senderName: 'John Doe',
    senderRole: 'Client',
    body: `Message ${id}`,
    isRead: true,
    sentAt: '2026-10-20T08:30:00Z',
    ...overrides,
  }
}

function page(data, { totalPages = 1, currentPage = 1 } = {}) {
  return { totalRecords: data.length, totalPages, currentPage, data }
}

// Routes fetch calls by method. `pages` maps page numbers to responses (or a function that returns
// the current pages, for a thread that changes); `post` decides each send's response, `read` each
// PATCH /read response.
function stubApi({
  pages = { 1: page([]) },
  post = async () => json(message('new'), 201),
  read = async () => json({ message: 'Messages marked as read.', updated: 0 }),
} = {}) {
  const fetchMock = vi.fn(async (url, options = {}) => {
    const method = options.method ?? 'GET'
    if (method === 'POST') return post(JSON.parse(options.body))
    if (method === 'PATCH') return read()
    const requested = Number(new URL(url, 'http://test').searchParams.get('page'))
    const current = typeof pages === 'function' ? pages() : pages
    return json(current[requested])
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function calls(fetchMock, method) {
  return fetchMock.mock.calls.filter(([, options]) => (options?.method ?? 'GET') === method)
}

function signIn(role, userId = ME) {
  endSession()
  startSession({ accessToken: 'token', refreshToken: 'refresh', role, userId, displayName: `${role} User` })
}

function renderThread(props = {}) {
  return render(
    <ToastProvider>
      <CaseMessages caseId={CASE_ID} {...props} />
    </ToastProvider>,
  )
}

function bodies() {
  return within(screen.getByRole('region', { name: 'Message history' }))
    .getAllByRole('listitem')
    .map((item) => item.querySelector('article > p:last-child').textContent)
}

function box() {
  return screen.getByLabelText('Your message')
}

function type(text) {
  fireEvent.change(box(), { target: { value: text } })
}

describe('CaseMessages', () => {
  beforeEach(() => signIn('Consultant'))

  it('lists the thread with sender, role and time, and marks my own messages', async () => {
    stubApi({
      pages: {
        1: page([
          message(1, { body: 'When is my appointment?' }),
          message(2, { senderId: ME, senderName: 'Consultant User', senderRole: 'Consultant', body: 'Thursday at 10.' }),
        ]),
      },
    })
    renderThread()

    const history = await screen.findByRole('region', { name: 'Message history' })
    const items = within(history).getAllByRole('listitem')
    expect(items).toHaveLength(2)
    expect(items[0].textContent).toContain('John Doe')
    expect(items[0].textContent).toContain('Client')
    expect(items[0].textContent).toContain('When is my appointment?')
    expect(items[0].textContent).not.toContain('(you)')
    expect(items[1].textContent).toContain('Consultant User (you)')
    expect(items[1].querySelector('time').getAttribute('dateTime')).toBe('2026-10-20T08:30:00Z')
  })

  it('shows messages in the order the API sends them, oldest first, never reversed', async () => {
    stubApi({
      pages: {
        1: page([
          message('a', { body: 'first', sentAt: '2026-10-20T08:00:00Z' }),
          message('b', { body: 'second', sentAt: '2026-10-20T09:00:00Z' }),
          message('c', { body: 'third', sentAt: '2026-10-20T10:00:00Z' }),
        ]),
      },
    })
    renderThread()

    await screen.findByText('third')
    expect(bodies()).toEqual(['first', 'second', 'third'])
  })

  it('shows the empty state when there are no messages', async () => {
    stubApi()
    renderThread({ emptyMessage: 'Say hello.' })

    expect(await screen.findByText('No messages yet')).toBeTruthy()
    expect(screen.getByText('Say hello.')).toBeTruthy()
  })

  it('loads the newest page when the thread has more than one, and can show earlier ones', async () => {
    const fetchMock = stubApi({
      pages: {
        1: page([message('old')], { totalPages: 2 }),
        2: page([message('new')], { totalPages: 2, currentPage: 2 }),
      },
    })
    renderThread()

    expect(await screen.findByText('Message new')).toBeTruthy()
    expect(screen.queryByText('Message old')).toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Show earlier messages' }))
    expect(await screen.findByText('Message old')).toBeTruthy()
    expect(bodies()).toEqual(['Message old', 'Message new'])
    expect(screen.queryByRole('button', { name: 'Show earlier messages' })).toBeNull()
    expect(calls(fetchMock, 'GET')).toHaveLength(3)
  })

  describe('read state', () => {
    it('marks the thread read once when it opens, with no body, and tells the header', async () => {
      const heard = vi.fn()
      window.addEventListener(MESSAGES_READ_EVENT, heard)
      const fetchMock = stubApi({
        pages: { 1: page([message(1, { isRead: false })]) },
        read: async () => json({ message: 'Messages marked as read.', updated: 1 }),
      })
      renderThread()

      await waitFor(() => expect(calls(fetchMock, 'PATCH')).toHaveLength(1))
      const [url, options] = calls(fetchMock, 'PATCH')[0]
      expect(url).toBe(READ_PATH)
      expect(options.body).toBeUndefined()
      await waitFor(() => expect(heard).toHaveBeenCalledTimes(1))
      window.removeEventListener(MESSAGES_READ_EVENT, heard)
    })

    it('tags messages that were unread when the thread opened as new, and only those', async () => {
      stubApi({
        pages: {
          1: page([
            message('seen', { body: 'Already read' }),
            message('fresh', { body: 'Just arrived', isRead: false }),
            message('mine', { senderId: ME, senderName: 'Consultant User', body: 'My reply', isRead: false }),
          ]),
        },
      })
      renderThread()

      const items = within(await screen.findByRole('region', { name: 'Message history' })).getAllByRole('listitem')
      expect(items.map((item) => item.textContent.includes('New'))).toEqual([false, true, false])
    })

    it('keeps the thread usable when marking read fails', async () => {
      stubApi({
        pages: { 1: page([message(1, { isRead: false })]) },
        read: async () => json({ message: 'Server error.' }, 500),
      })
      renderThread()

      expect(await screen.findByText('Message 1')).toBeTruthy()
      await waitFor(() => expect(screen.queryByRole('alert')).toBeNull())
      expect(box().disabled).toBe(false)
    })

    it('adds new messages that arrive while open at the bottom and marks them read again', async () => {
      let thread = [message(1, { body: 'Earlier', sentAt: '2026-10-20T08:00:00Z' })]
      const fetchMock = stubApi({ pages: () => ({ 1: page(thread) }) })
      renderThread({ pollInterval: 20 })

      await screen.findByText('Earlier')
      await waitFor(() => expect(calls(fetchMock, 'PATCH')).toHaveLength(1))

      thread = [...thread, message(2, { body: 'Reply from the client', isRead: false, sentAt: '2026-10-20T09:00:00Z' })]
      expect(await screen.findByText('Reply from the client')).toBeTruthy()
      expect(bodies()).toEqual(['Earlier', 'Reply from the client'])
      await waitFor(() => expect(calls(fetchMock, 'PATCH')).toHaveLength(2))
    })

    it('does not mark read again when only my own messages come back from a check', async () => {
      const thread = [message(1)]
      const fetchMock = stubApi({ pages: () => ({ 1: page(thread) }) })
      renderThread({ pollInterval: 20 })

      await screen.findByText('Message 1')
      thread.push(message(2, { senderId: ME, senderName: 'Consultant User', body: 'Sent elsewhere', isRead: false }))
      expect(await screen.findByText('Sent elsewhere')).toBeTruthy()
      await new Promise((resolve) => setTimeout(resolve, 60))
      expect(calls(fetchMock, 'PATCH')).toHaveLength(1)
    })
  })

  describe('as an admin', () => {
    beforeEach(() => signIn('Admin', 'admin-1'))

    it('shows the thread read-only: no send box, no read call, no new tags', async () => {
      const fetchMock = stubApi({ pages: { 1: page([message(1, { isRead: false })]) } })
      renderThread({ emptyMessage: 'Say hello.' })

      expect(await screen.findByText('Message 1')).toBeTruthy()
      expect(screen.getByText(/You can read this thread\. Only the client and their consultant send messages here\./)).toBeTruthy()
      expect(screen.queryByLabelText('Your message')).toBeNull()
      expect(screen.queryByRole('button', { name: 'Send' })).toBeNull()
      expect(screen.queryByText('New')).toBeNull()
      await new Promise((resolve) => setTimeout(resolve, 20))
      expect(calls(fetchMock, 'PATCH')).toHaveLength(0)
    })
  })

  it('does not send an empty or whitespace-only message', async () => {
    const fetchMock = stubApi()
    renderThread()
    await screen.findByText('No messages yet')

    type('   \n ')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(screen.getByText('Write a message before sending.')).toBeTruthy()
    expect(box().getAttribute('aria-invalid')).toBe('true')
    expect(calls(fetchMock, 'POST')).toHaveLength(0)
  })

  it('counts code points and blocks a message over 2000 of them', async () => {
    const fetchMock = stubApi()
    renderThread()
    await screen.findByText('No messages yet')

    type('😀'.repeat(2001))
    expect(screen.getByText(/^2\s001 \/ 2\s000 characters$/)).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(screen.getByText(/^Messages can be up to 2\s000 characters\./)).toBeTruthy()
    expect(calls(fetchMock, 'POST')).toHaveLength(0)
  })

  it('sends the trimmed text, appends the new message at the bottom, confirms with a toast and focuses the box', async () => {
    const fetchMock = stubApi({
      pages: { 1: page([message(1, { sentAt: '2026-10-20T08:00:00Z' })]) },
      post: async (body) =>
        json(message(2, { senderId: ME, senderName: 'Consultant User', body: body.body, sentAt: '2026-10-21T08:00:00Z' }), 201),
    })
    renderThread()
    await screen.findByText('Message 1')

    type('  Your visa was approved.  ')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(await screen.findByText('Your visa was approved.')).toBeTruthy()
    expect(JSON.parse(calls(fetchMock, 'POST')[0][1].body)).toEqual({ body: 'Your visa was approved.' })
    expect(bodies()).toEqual(['Message 1', 'Your visa was approved.'])
    expect(box().value).toBe('')
    expect(within(screen.getByRole('status')).getByText('Message sent.')).toBeTruthy()
    await waitFor(() => expect(document.activeElement).toBe(box()))
  })

  it('sends with Ctrl+Enter', async () => {
    const fetchMock = stubApi()
    renderThread()
    await screen.findByText('No messages yet')

    type('Quick question')
    fireEvent.keyDown(box(), { key: 'Enter', ctrlKey: true })

    await waitFor(() => expect(calls(fetchMock, 'POST')).toHaveLength(1))
  })

  it('disables Send while the message is on its way', async () => {
    const pending = deferred()
    const fetchMock = stubApi({ post: () => pending.promise })
    renderThread()
    await screen.findByText('No messages yet')

    type('Hello')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    const button = await screen.findByRole('button', { name: 'Sending…' })
    expect(button.disabled).toBe(true)
    // A second Ctrl+Enter while sending must not send twice.
    fireEvent.keyDown(box(), { key: 'Enter', ctrlKey: true })
    expect(calls(fetchMock, 'POST')).toHaveLength(1)

    pending.resolve(json(message('sent', { body: 'Hello' }), 201))
    expect(await screen.findByRole('button', { name: 'Send' })).toBeTruthy()
  })

  it('shows the API field errors under the box', async () => {
    stubApi({
      post: async () => json({ message: 'Validation failed.', errors: ['Message body must be 2000 characters or fewer.'] }, 400),
    })
    renderThread()
    await screen.findByText('No messages yet')

    type('Hello')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    const error = await screen.findByText('Message body must be 2000 characters or fewer.')
    expect(box().getAttribute('aria-describedby')).toContain(error.parentElement.id)
    expect(box().value).toBe('Hello')
    expect(box().disabled).toBe(false)
  })

  it('shows the API reason and switches the box off when an archived case refuses the message', async () => {
    stubApi({ post: async () => json({ message: 'Messages cannot be sent on an archived case.' }, 400) })
    renderThread()
    await screen.findByText('No messages yet')

    type('Hello')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect((await screen.findByRole('alert')).textContent).toBe('Messages cannot be sent on an archived case.')
    expect(box().disabled).toBe(true)
    expect(screen.getByRole('button', { name: 'Send' }).disabled).toBe(true)
  })

  it('switches the box off with a plain reason when the API forbids the send', async () => {
    stubApi({ post: async () => json(null, 403) })
    renderThread()
    await screen.findByText('No messages yet')

    type('Hello')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect((await screen.findByRole('alert')).textContent).toBe('You no longer have access to messages on this case.')
    expect(box().disabled).toBe(true)
  })

  it('keeps the box and the text when the send fails for another reason', async () => {
    stubApi({ post: async () => json({ message: 'Case not found.' }, 404) })
    renderThread()
    await screen.findByText('No messages yet')

    type('Hello')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect((await screen.findByRole('alert')).textContent).toBe('Case not found.')
    expect(box().value).toBe('Hello')
    expect(box().disabled).toBe(false)
  })

  it('starts switched off for an archived case', async () => {
    stubApi()
    renderThread({ archived: true })
    await screen.findByText('No messages yet')

    expect(screen.getByText('This case is archived, so no new messages can be sent.')).toBeTruthy()
    expect(box().disabled).toBe(true)
  })

  it('offers a retry when the thread cannot be loaded', async () => {
    let fail = true
    vi.stubGlobal(
      'fetch',
      vi.fn(async (_url, options = {}) => {
        if (options.method === 'PATCH') return json({ updated: 0 })
        return fail ? json({ message: 'Case not found.' }, 404) : json(page([message(1)]))
      }),
    )
    renderThread()

    expect((await screen.findByRole('alert')).textContent).toContain('Case not found.')
    fail = false
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByText('Message 1')).toBeTruthy()
  })

  it('words a 403 on loading plainly', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => json(null, 403)))
    renderThread()

    expect((await screen.findByRole('alert')).textContent).toContain('You no longer have access to messages on this case.')
  })
})
