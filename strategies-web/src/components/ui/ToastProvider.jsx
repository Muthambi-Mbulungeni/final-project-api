import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { createPortal } from 'react-dom'
import { Check, ShieldAlert, X } from 'lucide-react'
import { ToastContext } from './toastContext'
import { toastHost } from './toastHost'

const TONES = {
  success: { icon: Check, iconClasses: 'bg-emerald-50 text-emerald-700' },
  error: { icon: ShieldAlert, iconClasses: 'bg-brand-50 text-brand-700' },
}

export default function ToastProvider({ children }) {
  const [toasts, setToasts] = useState([])
  const nextId = useRef(0)

  const dismiss = useCallback((id) => {
    setToasts((current) => current.filter((toast) => toast.id !== id))
  }, [])

  const show = useCallback(
    (message, { tone = 'success', duration = 4000 } = {}) => {
      nextId.current += 1
      const id = nextId.current
      setToasts((current) => [...current, { id, message, tone }])
      if (duration > 0) {
        setTimeout(() => dismiss(id), duration)
      }
      return id
    },
    [dismiss],
  )

  const value = useMemo(() => ({ show, dismiss }), [show, dismiss])

  // Follows dialogs as they open and close, so the live region is already inside an open panel
  // before its first toast arrives (a region added together with its message is often not read out).
  // Removed nodes are watched too: a dialog that unmounts while open is taken out of the page before
  // its close() runs, so no "open" change is ever seen and the region would stay in the detached node.
  const [host, setHost] = useState(null)
  useEffect(() => {
    const update = () => setHost(toastHost(document))
    update()
    const observer = new MutationObserver(update)
    observer.observe(document.body, { subtree: true, childList: true, attributeFilter: ['open'] })
    return () => observer.disconnect()
  }, [])

  const region = (
    <div
      aria-live="polite"
      className="pointer-events-none fixed right-4 bottom-4 z-50 flex w-[calc(100%-2rem)] max-w-sm flex-col gap-2"
    >
      {toasts.map((toast) => {
        const { icon: Icon, iconClasses } = TONES[toast.tone] ?? TONES.success
        return (
          <div
            key={toast.id}
            role={toast.tone === 'error' ? 'alert' : 'status'}
            className="pointer-events-auto flex items-start gap-3 rounded-lg border border-stone-200 bg-white px-4 py-3 shadow-md shadow-stone-900/10"
          >
            <span className={`mt-0.5 flex size-6 shrink-0 items-center justify-center rounded-full ${iconClasses}`}>
              <Icon className="size-3.5" strokeWidth={2.25} aria-hidden="true" />
            </span>
            <p className="flex-1 text-sm text-stone-800">{toast.message}</p>
            <button
              type="button"
              onClick={() => dismiss(toast.id)}
              className="rounded p-0.5 text-stone-500 hover:bg-stone-100 hover:text-stone-700"
              aria-label="Dismiss notification"
            >
              <X className="size-4" strokeWidth={1.75} />
            </button>
          </div>
        )
      })}
    </div>
  )

  return (
    <ToastContext.Provider value={value}>
      {children}
      {host ? createPortal(region, host) : region}
    </ToastContext.Provider>
  )
}
