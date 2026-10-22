import { describe, expect, it } from 'vitest'
import { toastHost } from './toastHost'

function fakeDocument(openDialogs) {
  const body = { tagName: 'BODY' }
  return {
    body,
    querySelectorAll: (selector) => (selector === 'dialog[open]' ? openDialogs : []),
  }
}

describe('toastHost', () => {
  it('renders toasts in the page body when no dialog is open', () => {
    const doc = fakeDocument([])
    expect(toastHost(doc)).toBe(doc.body)
  })

  it('renders toasts inside an open dialog, so they show above the case panel', () => {
    const panel = { tagName: 'DIALOG', id: 'case-panel' }
    expect(toastHost(fakeDocument([panel]))).toBe(panel)
  })

  it('uses the last open dialog when one is open over another', () => {
    const panel = { tagName: 'DIALOG', id: 'case-panel' }
    const confirm = { tagName: 'DIALOG', id: 'confirm' }
    expect(toastHost(fakeDocument([panel, confirm]))).toBe(confirm)
  })
})
