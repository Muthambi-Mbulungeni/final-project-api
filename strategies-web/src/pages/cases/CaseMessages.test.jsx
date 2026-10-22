// @vitest-environment jsdom
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { deferred, json } from '../../test/dom'
import { endSession, startSession } from '../../lib/session'
import CaseMessages from './CaseMessages'

const CASE_ID = 'case-1'
const ME = 'consultant-1'

function message(id, overrides = {}) {
  return {
    id,
    caseId: CASE_ID,
    senderId: 'client-1',
    senderName: 'John Doe',
    senderRole: 'Client',
    body: `Message ${id}`,
    isRead: false,
    sentAt: '2026-10-20T08:30:00Z',
    ...overrides,
  }
}

function page(data, { totalPages = 1, currentPage = 1 } = {}) {
  return { totalRecords: data.length, totalPages, currentPage, data }
}

// Routes fetch calls by method; `post` decides each send's response.
function stubApi({ pages = { 1: page([]) }, post = async () => json(message('new'), 201) } = {}) {
  const fetchMock = vi.fn(async (url, options = {}) => {
    if ((options.method ?? 'GET') === 'POST') return post(JSON.parse(options.body))
    const requested = Number(new URL(url, 'http://test').searchParams.get('page'))
    return json(pages[requested])
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function posts(fetchMock) {
  return fetchMock.mock.calls.filter(([, options]) => options?.method === 'POST')
}

function box() {
  return screen.getByLabelText('Your message')
}

function type(text) {
  fireEvent.change(box(), { target: { value: text } })
}

describe('CaseMessages', () => {
  beforeEach(() => {
    endSession()
    startSession({ accessToken: 'token', refreshToken: 'refresh', role: 'Consultant', userId: ME, displayName: 'Consultant User' })
  })

  it('lists the thread with sender, role and time, and marks my own messages', async () => {
    stubApi({
      pages: {
        1: page([
          message(1, { body: 'When is my appointment?' }),
          message(2, { senderId: ME, senderName: 'Consultant User', senderRole: 'Consultant', body: 'Thursday at 10.' }),
        ]),
      },
    })
    render(<CaseMessages caseId={CASE_ID} />)

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

  it('shows the empty state when there are no messages', async () => {
    stubApi()
    render(<CaseMessages caseId={CASE_ID} emptyMessage="Say hello." />)

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
    render(<CaseMessages caseId={CASE_ID} />)

    expect(await screen.findByText('Message new')).toBeTruthy()
    expect(screen.queryByText('Message old')).toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Show earlier messages' }))
    expect(await screen.findByText('Message old')).toBeTruthy()
    const bodies = screen.getAllByRole('listitem').map((item) => item.textContent)
    expect(bodies[0]).toContain('Message old')
    expect(screen.queryByRole('button', { name: 'Show earlier messages' })).toBeNull()
    expect(fetchMock).toHaveBeenCalledTimes(3)
  })

  it('does not send an empty or whitespace-only message', async () => {
    const fetchMock = stubApi()
    render(<CaseMessages caseId={CASE_ID} />)
    await screen.findByText('No messages yet')

    type('   \n ')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(screen.getByText('Write a message before sending.')).toBeTruthy()
    expect(box().getAttribute('aria-invalid')).toBe('true')
    expect(posts(fetchMock)).toHaveLength(0)
  })

  it('counts code points and blocks a message over 2000 of them', async () => {
    const fetchMock = stubApi()
    render(<CaseMessages caseId={CASE_ID} />)
    await screen.findByText('No messages yet')

    type('😀'.repeat(2001))
    expect(screen.getByText(/^2\s001 \/ 2\s000 characters$/)).toBeTruthy()
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(screen.getByText(/^Messages can be up to 2\s000 characters\./)).toBeTruthy()
    expect(posts(fetchMock)).toHaveLength(0)
  })

  it('sends the trimmed text, appends the new message, clears the box and focuses it', async () => {
    const fetchMock = stubApi({
      pages: { 1: page([message(1)]) },
      post: async (body) => json(message(2, { senderId: ME, senderName: 'Consultant User', body: body.body }), 201),
    })
    render(<CaseMessages caseId={CASE_ID} />)
    await screen.findByText('Message 1')

    type('  Your visa was approved.  ')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect(await screen.findByText('Your visa was approved.')).toBeTruthy()
    expect(JSON.parse(posts(fetchMock)[0][1].body)).toEqual({ body: 'Your visa was approved.' })
    expect(screen.getAllByRole('listitem')).toHaveLength(2)
    expect(box().value).toBe('')
    expect(screen.getByRole('status').textContent).toBe('Message sent.')
    await waitFor(() => expect(document.activeElement).toBe(box()))
  })

  it('sends with Ctrl+Enter', async () => {
    const fetchMock = stubApi()
    render(<CaseMessages caseId={CASE_ID} />)
    await screen.findByText('No messages yet')

    type('Quick question')
    fireEvent.keyDown(box(), { key: 'Enter', ctrlKey: true })

    await waitFor(() => expect(posts(fetchMock)).toHaveLength(1))
  })

  it('disables Send while the message is on its way', async () => {
    const pending = deferred()
    const fetchMock = stubApi({ post: () => pending.promise })
    render(<CaseMessages caseId={CASE_ID} />)
    await screen.findByText('No messages yet')

    type('Hello')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    const button = await screen.findByRole('button', { name: 'Sending…' })
    expect(button.disabled).toBe(true)
    // A second Ctrl+Enter while sending must not send twice.
    fireEvent.keyDown(box(), { key: 'Enter', ctrlKey: true })
    expect(posts(fetchMock)).toHaveLength(1)

    pending.resolve(json(message('sent', { body: 'Hello' }), 201))
    expect(await screen.findByRole('button', { name: 'Send' })).toBeTruthy()
  })

  it('shows the API field errors under the box', async () => {
    stubApi({
      post: async () => json({ message: 'Validation failed.', errors: ['Body must be 2000 characters or fewer.'] }, 400),
    })
    render(<CaseMessages caseId={CASE_ID} />)
    await screen.findByText('No messages yet')

    type('Hello')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    const error = await screen.findByText('Body must be 2000 characters or fewer.')
    expect(box().getAttribute('aria-describedby')).toContain(error.parentElement.id)
    expect(box().value).toBe('Hello')
    expect(box().disabled).toBe(false)
  })

  it('switches the box off when the API refuses the message without field errors', async () => {
    stubApi({ post: async () => json({ message: 'This case is archived.' }, 400) })
    render(<CaseMessages caseId={CASE_ID} />)
    await screen.findByText('No messages yet')

    type('Hello')
    fireEvent.click(screen.getByRole('button', { name: 'Send' }))

    expect((await screen.findByRole('alert')).textContent).toBe('This case is archived.')
    expect(box().disabled).toBe(true)
    expect(screen.getByRole('button', { name: 'Send' }).disabled).toBe(true)
  })

  it('starts switched off for an archived case', async () => {
    stubApi()
    render(<CaseMessages caseId={CASE_ID} archived />)
    await screen.findByText('No messages yet')

    expect(screen.getByText('This case is archived, so no new messages can be sent.')).toBeTruthy()
    expect(box().disabled).toBe(true)
  })

  it('offers a retry when the thread cannot be loaded', async () => {
    let fail = true
    vi.stubGlobal(
      'fetch',
      vi.fn(async () => (fail ? json({ message: 'Case not found.' }, 404) : json(page([message(1)])))),
    )
    render(<CaseMessages caseId={CASE_ID} />)

    expect((await screen.findByRole('alert')).textContent).toContain('Case not found.')
    fail = false
    fireEvent.click(screen.getByRole('button', { name: 'Try again' }))
    expect(await screen.findByText('Message 1')).toBeTruthy()
  })
})
