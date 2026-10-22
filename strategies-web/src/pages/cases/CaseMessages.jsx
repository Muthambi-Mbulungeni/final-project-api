import { useEffect, useLayoutEffect, useRef, useState } from 'react'
import EmptyState from '../../components/ui/EmptyState'
import ErrorNotice from '../../components/ui/ErrorNotice'
import { textareaClasses } from '../../components/ui/fieldStyles'
import { resolveEnum } from '../../config/enums'
import { formatDateTime } from '../../lib/format'
import {
  isOwnMessage,
  loadLatestMessages,
  loadMessagesPage,
  MESSAGE_MAX_LENGTH,
  mergeMessages,
  messageError,
  messageLength,
  sendErrorOutcome,
  sendMessage,
} from '../../lib/messages'
import { getSession } from '../../lib/session'

const ARCHIVED_MESSAGE = 'This case is archived, so no new messages can be sent.'
const LOADING = { status: 'loading', messages: [], page: 1, error: null }

function MessageItem({ message, own }) {
  const role = resolveEnum('userRole', message.senderRole)?.label

  return (
    <li className={`flex ${own ? 'justify-end' : ''}`}>
      <article
        className={`max-w-[85%] min-w-0 rounded-lg border px-3.5 py-2.5 ${
          own ? 'border-brand-200 bg-brand-50' : 'border-stone-200 bg-white'
        }`}
      >
        <p className="flex flex-wrap items-baseline gap-x-2 text-xs text-stone-500">
          <span className="font-medium text-stone-900">
            {message.senderName || 'Unknown sender'}
            {own && <span className="sr-only"> (you)</span>}
          </span>
          {role && <span>{role}</span>}
          <time dateTime={message.sentAt}>{formatDateTime(message.sentAt)}</time>
        </p>
        <p className="mt-1 text-sm break-words whitespace-pre-wrap text-stone-800">{message.body}</p>
      </article>
    </li>
  )
}

// The message thread for one case and a box to send a new message. Used by staff in the case
// panel and by the client on their dashboard. Mount it per case (key by case id), so a newly
// opened case never shows the previous thread. `archived` switches the send box off up front;
// the API can also refuse a message (400 without field errors), which switches it off the same way.
export default function CaseMessages({ caseId, archived = false, emptyMessage }) {
  const userId = getSession()?.id
  const [thread, setThread] = useState(LOADING)
  const [attempt, setAttempt] = useState(0)
  const [earlier, setEarlier] = useState({ loading: false, error: null })
  const [draft, setDraft] = useState('')
  const [sending, setSending] = useState(false)
  const [fieldErrors, setFieldErrors] = useState([])
  const [formError, setFormError] = useState(null)
  const [closedMessage, setClosedMessage] = useState(null)
  const [announcement, setAnnouncement] = useState('')
  const historyRef = useRef(null)
  const textareaRef = useRef(null)
  // Set before earlier messages are added above, so the reader's place in the thread is kept.
  const keepScrollFromBottom = useRef(null)

  const fieldId = `message-${caseId}`
  const hintId = `${fieldId}-hint`
  const countId = `${fieldId}-count`
  const errorId = `${fieldId}-error`
  const closed = archived || closedMessage !== null
  const length = messageLength(draft)
  const overLimit = length > MESSAGE_MAX_LENGTH
  const messages = thread.messages
  const lastId = messages.at(-1)?.id

  useEffect(() => {
    const controller = new AbortController()
    loadLatestMessages(caseId, { signal: controller.signal })
      .then(({ messages: loaded, page }) => setThread({ status: 'ready', messages: loaded, page, error: null }))
      .catch((error) => {
        if (error.name !== 'AbortError') setThread({ ...LOADING, status: 'error', error })
      })
    return () => controller.abort()
  }, [caseId, attempt])

  // Keeps the newest message in view when the thread loads and after each new one.
  useLayoutEffect(() => {
    const history = historyRef.current
    if (!history) return
    if (keepScrollFromBottom.current !== null) {
      history.scrollTop = history.scrollHeight - keepScrollFromBottom.current
      keepScrollFromBottom.current = null
    } else {
      history.scrollTop = history.scrollHeight
    }
  }, [lastId, messages.length])

  function retry() {
    setThread(LOADING)
    setAttempt((count) => count + 1)
  }

  async function showEarlier() {
    setEarlier({ loading: true, error: null })
    try {
      const older = await loadMessagesPage(caseId, thread.page - 1)
      const history = historyRef.current
      if (history) keepScrollFromBottom.current = history.scrollHeight - history.scrollTop
      setThread((current) => ({ ...current, messages: mergeMessages(older, current.messages), page: current.page - 1 }))
      setEarlier({ loading: false, error: null })
    } catch (error) {
      setEarlier({ loading: false, error: error.message })
    }
  }

  async function submit() {
    if (sending || closed) return
    setFormError(null)
    setAnnouncement('')

    const error = messageError(draft)
    if (error) {
      setFieldErrors([error])
      textareaRef.current?.focus()
      return
    }

    setFieldErrors([])
    setSending(true)
    try {
      const created = await sendMessage(caseId, draft)
      setThread((current) => ({ ...current, messages: mergeMessages(current.messages, [created]) }))
      setDraft('')
      setAnnouncement('Message sent.')
    } catch (sendError) {
      const outcome = sendErrorOutcome(sendError)
      if (outcome.kind === 'field') setFieldErrors(outcome.errors)
      else if (outcome.kind === 'closed') setClosedMessage(outcome.message)
      else setFormError(outcome.message)
    } finally {
      setSending(false)
    }
    // Focus goes back to the box for the next message (or to fix this one). A switched-off box
    // cannot take focus, so the alert explaining why is announced instead.
    requestAnimationFrame(() => textareaRef.current?.focus())
  }

  function handleSubmit(event) {
    event.preventDefault()
    submit()
  }

  function handleKeyDown(event) {
    if (event.key === 'Enter' && (event.ctrlKey || event.metaKey)) {
      event.preventDefault()
      submit()
    }
  }

  let history
  if (thread.status === 'error') {
    history = <ErrorNotice message={thread.error.message} onRetry={retry} />
  } else if (thread.status === 'loading') {
    history = <p className="py-6 text-center text-sm text-stone-500">Loading messages…</p>
  } else if (messages.length === 0) {
    history = <EmptyState title="No messages yet" message={emptyMessage} />
  } else {
    history = (
      <div
        ref={historyRef}
        role="region"
        aria-label="Message history"
        // Scrollable, so keyboard users must be able to reach it to scroll (WCAG 2.1.1).
        tabIndex={0}
        className="max-h-96 space-y-3 overflow-y-auto rounded-lg bg-stone-50 p-3"
      >
        {thread.page > 1 && (
          <div className="text-center">
            <button
              type="button"
              onClick={showEarlier}
              disabled={earlier.loading}
              className="rounded-md border border-stone-300 bg-white px-3 py-1.5 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50 disabled:opacity-60"
            >
              {earlier.loading ? 'Loading…' : 'Show earlier messages'}
            </button>
            {earlier.error && (
              <p role="alert" className="mt-2 text-sm text-brand-700">
                {earlier.error}
              </p>
            )}
          </div>
        )}
        <ol className="space-y-3">
          {messages.map((message) => (
            <MessageItem key={message.id} message={message} own={isOwnMessage(message, userId)} />
          ))}
        </ol>
      </div>
    )
  }

  const describedBy = [hintId, countId, fieldErrors.length ? errorId : null].filter(Boolean).join(' ')

  return (
    <div className="space-y-4">
      {history}

      {closedMessage && <ErrorNotice message={closedMessage} />}
      {archived && !closedMessage && (
        <p className="rounded-md border border-stone-200 bg-stone-50 px-3 py-2 text-sm text-stone-700">{ARCHIVED_MESSAGE}</p>
      )}

      <form onSubmit={handleSubmit} noValidate className="space-y-2">
        <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
          <label htmlFor={fieldId} className="block text-sm font-medium text-stone-800">
            Your message
          </label>
          {/* Hidden on phones, which have no Ctrl key; still read out as the field's description. */}
          <p id={hintId} className="hidden text-xs text-stone-500 sm:block">
            Press Ctrl+Enter to send.
          </p>
        </div>
        <textarea
          id={fieldId}
          ref={textareaRef}
          rows={3}
          value={draft}
          // Read-only while sending, not disabled, so focus stays in the box.
          readOnly={sending}
          disabled={closed}
          onChange={(event) => {
            setDraft(event.target.value)
            if (fieldErrors.length) setFieldErrors([])
          }}
          onKeyDown={handleKeyDown}
          aria-invalid={fieldErrors.length ? true : undefined}
          aria-describedby={describedBy}
          className={`${textareaClasses(fieldErrors.length > 0)} disabled:cursor-not-allowed disabled:bg-stone-50 disabled:text-stone-500`}
        />
        {fieldErrors.length > 0 && (
          <div id={errorId} className="space-y-1">
            {fieldErrors.map((message) => (
              <p key={message} className="text-sm text-brand-700">
                {message}
              </p>
            ))}
          </div>
        )}
        {formError && (
          <p role="alert" className="text-sm text-brand-700">
            {formError}
          </p>
        )}
        <div className="flex items-center justify-between gap-3">
          <p id={countId} className={`text-xs ${overLimit ? 'font-medium text-brand-700' : 'text-stone-500'}`}>
            {length.toLocaleString('en-ZA')} / {MESSAGE_MAX_LENGTH.toLocaleString('en-ZA')} characters
          </p>
          <button
            type="submit"
            disabled={sending || closed}
            className={`rounded-md bg-brand-600 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:opacity-60 ${
              sending ? 'disabled:cursor-wait' : 'disabled:cursor-not-allowed'
            }`}
          >
            {sending ? 'Sending…' : 'Send'}
          </button>
        </div>
        <p role="status" className="sr-only">
          {announcement}
        </p>
      </form>
    </div>
  )
}
