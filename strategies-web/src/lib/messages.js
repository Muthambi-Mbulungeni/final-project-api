// Messages on one case, between the client and the practice (MessagesController, FR-3.5).
// GET   /api/cases/{caseId}/messages?page&pageSize returns
//       { totalRecords, totalPages, currentPage, data: [{ id, caseId, senderId, senderName, senderRole, body, isRead, sentAt }] },
//       oldest first. Shown in that order; new messages go at the bottom.
// POST  /api/cases/{caseId}/messages with { body } returns 201 and the created message.
//       400 { message, errors } is a problem with the text; 400 { message } alone means the case
//       cannot take messages (it is archived).
// PATCH /api/cases/{caseId}/messages/read takes no body and marks every message on the case that
//       the caller did not send as read. Admins get 403: they read threads but never change them.
import { apiGet, apiPatch, apiPost } from './api'

export const MESSAGE_MAX_LENGTH = 2000
export const MESSAGES_PAGE_SIZE = 50
// How often an open thread checks for new messages.
export const MESSAGES_POLL_MS = 30_000
// Sent on window after a thread is marked read, so the unread count in the header can reload.
export const MESSAGES_READ_EVENT = 'strategies:messages-read'

const FORBIDDEN_MESSAGE = 'You no longer have access to messages on this case.'

export function messagesPath(caseId, { page = 1, pageSize = MESSAGES_PAGE_SIZE } = {}) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return `/api/cases/${encodeURIComponent(caseId)}/messages?${params}`
}

// Characters as people count them: an emoji or an accented letter written as one code point is
// one, not the two UTF-16 units that String.length reports. Same rule as the API.
export function messageLength(text) {
  return [...String(text ?? '')].length
}

// Returns an error message for the text in the send box, or undefined when it can be sent.
// The limit applies to the trimmed text, which is what the API stores.
export function messageError(text) {
  const trimmed = String(text ?? '').trim()
  if (!trimmed) return 'Write a message before sending.'
  if (messageLength(trimmed) > MESSAGE_MAX_LENGTH) {
    return `Messages can be up to ${MESSAGE_MAX_LENGTH.toLocaleString('en-ZA')} characters. Shorten yours and try again.`
  }
  return undefined
}

export function messageBody(text) {
  return { body: String(text ?? '').trim() }
}

// Admins read every thread but take no part in it: no send box, and opening a thread must not
// mark the client's or consultant's messages as read.
export function canTakePart(role) {
  return role === 'consultant' || role === 'client'
}

// Text for a thread that cannot be loaded. A bare 403 from the API has no message of its own.
export function threadErrorMessage(error) {
  if (error?.status === 403) return FORBIDDEN_MESSAGE
  return error?.message || 'Messages could not be loaded.'
}

// Turns a failed send into what the box should show: { kind: 'field' | 'closed' | 'form', message, errors }.
// 'field': problems with the text, shown under the box. 'closed': the case cannot take messages,
// so the box is switched off. 'form': anything else, shown under the box while it stays usable.
// 401 never gets here as a plain failure: api.js renews the session or signs the person out.
export function sendErrorOutcome(error) {
  if (error?.status === 400) {
    const errors = Array.isArray(error.errors) ? error.errors.filter(Boolean) : []
    if (errors.length) return { kind: 'field', errors }
    return { kind: 'closed', message: error.message || 'This case is not taking new messages.' }
  }
  if (error?.status === 403) return { kind: 'closed', message: FORBIDDEN_MESSAGE }
  return { kind: 'form', message: error?.message || 'Your message was not sent. Please try again.' }
}

function sentTime(message) {
  const time = Date.parse(message?.sentAt)
  return Number.isNaN(time) ? 0 : time
}

// Adds messages to a thread without showing one twice (a page fetched again can repeat messages
// already on screen) and keeps it oldest first: a reply that arrives by polling after your own
// send still lands in the order it was sent. The sort is stable, so the API's order is kept for ties.
export function mergeMessages(existing, incoming) {
  const seen = new Set(existing.map((message) => message.id))
  const added = incoming.filter((message) => message && !seen.has(message.id))
  if (!added.length) return existing
  return [...existing, ...added].sort((a, b) => sentTime(a) - sentTime(b))
}

export function isOwnMessage(message, userId) {
  return Boolean(userId) && String(message?.senderId) === String(userId)
}

// A message someone else sent that was still unread when it was loaded.
export function isNewIncoming(message, userId) {
  return !isOwnMessage(message, userId) && message?.isRead === false
}

function pageOf(response) {
  return {
    messages: Array.isArray(response?.data) ? response.data : [],
    totalPages: Math.max(1, Number(response?.totalPages) || 1),
  }
}

// Loads the newest page of a thread. The list is oldest first, so page 1 holds the oldest messages:
// when there is more than one page, the last one is fetched instead. Returns { messages, page }.
// Earlier pages never change when messages are added, so "Show earlier messages" can load page - 1.
export async function loadLatestMessages(caseId, { signal } = {}) {
  const first = pageOf(await apiGet(messagesPath(caseId), { signal }))
  if (first.totalPages === 1) return { messages: first.messages, page: 1 }

  const last = pageOf(await apiGet(messagesPath(caseId, { page: first.totalPages }), { signal }))
  return { messages: last.messages, page: first.totalPages }
}

// Loads what is new since `fromPage` was the newest page: that page again (it may have filled up)
// and every page after it. Returns { messages, page } with page = the newest page now.
export async function loadNewerMessages(caseId, fromPage, { signal } = {}) {
  const first = pageOf(await apiGet(messagesPath(caseId, { page: fromPage }), { signal }))
  const messages = [...first.messages]
  for (let page = fromPage + 1; page <= first.totalPages; page += 1) {
    messages.push(...pageOf(await apiGet(messagesPath(caseId, { page }), { signal })).messages)
  }
  return { messages, page: Math.max(fromPage, first.totalPages) }
}

export async function loadMessagesPage(caseId, page) {
  return pageOf(await apiGet(messagesPath(caseId, { page }))).messages
}

export function sendMessage(caseId, text) {
  return apiPost(`/api/cases/${encodeURIComponent(caseId)}/messages`, messageBody(text))
}

// Marks the thread read for the signed-in client or consultant, then tells the header's unread
// count to reload. Returns how many messages changed.
export async function markThreadRead(caseId) {
  const result = await apiPatch(`/api/cases/${encodeURIComponent(caseId)}/messages/read`)
  const updated = Number(result?.updated) || 0
  if (updated > 0 && typeof window !== 'undefined') window.dispatchEvent(new Event(MESSAGES_READ_EVENT))
  return updated
}
