// Shared set-up for component tests. A test file opts in to the browser-like environment with
// `// @vitest-environment jsdom` on its first line and imports this file; logic tests stay in node.
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// jsdom has no <dialog> methods. These stand-ins only toggle the open attribute, which is all the
// components read; the browser's own focus trap and Escape handling are not simulated
// (tests fire the native "cancel" event to stand in for Escape).
if (typeof HTMLDialogElement !== 'undefined' && !HTMLDialogElement.prototype.showModal) {
  HTMLDialogElement.prototype.showModal = function showModal() {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.close = function close() {
    this.removeAttribute('open')
    this.dispatchEvent(new Event('close'))
  }
}

// Not part of jsdom either; components call it to keep the newest message in view.
if (typeof Element !== 'undefined' && !Element.prototype.scrollIntoView) {
  Element.prototype.scrollIntoView = function scrollIntoView() {}
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

// A fetch Response with a JSON body, as the API sends it.
export function json(body, status = 200) {
  return new Response(body === null ? '' : JSON.stringify(body), {
    status,
    headers: { 'Content-Type': 'application/json' },
  })
}

// A promise with its resolve function exposed, to hold a request open while a test looks at the
// pending state.
export function deferred() {
  let resolve
  const promise = new Promise((done) => {
    resolve = done
  })
  return { promise, resolve }
}
