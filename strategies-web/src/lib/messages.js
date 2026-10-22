// Messages on one case, between the client and the practice.
// GET  /api/cases/{caseId}/messages?page&pageSize returns
//      { totalRecords, totalPages, currentPage, data: [{ id, caseId, senderId, senderName, senderRole, body, isRead, sentAt }] },
//      oldest first.
// POST /api/cases/{caseId}/messages with { body } returns 201 and the created message.
//      400 { message, errors } is a problem with the text; 400 { message } alone means the case
//      cannot take messages (e.g. it is archived).
// Contract agreed with the API team; the endpoints are not built yet.
import { apiGet, apiPost } from './api'

export const MESSAGE_MAX_LENGTH = 2000
export const MESSAGES_PAGE_SIZE = 50

const TOO_MANY_MESSAGES = 'You are sending messages too quickly. Wait a minute and try again.'

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
  if (error?.status === 429) return { kind: 'form', message: TOO_MANY_MESSAGES }
  return { kind: 'form', message: error?.message || 'Your message was not sent. Please try again.' }
}

// Adds messages to a thread, oldest first, without showing one twice (a page fetched after a send
// can repeat a message that is already on screen).
export function mergeMessages(existing, incoming) {
  const seen = new Set(existing.map((message) => message.id))
  const added = incoming.filter((message) => message && !seen.has(message.id))
  return added.length ? [...existing, ...added] : existing
}

export function isOwnMessage(message, userId) {
  return Boolean(userId) && String(message?.senderId) === String(userId)
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

export async function loadMessagesPage(caseId, page) {
  return pageOf(await apiGet(messagesPath(caseId, { page }))).messages
}

export function sendMessage(caseId, text) {
  return apiPost(`/api/cases/${encodeURIComponent(caseId)}/messages`, messageBody(text))
}
