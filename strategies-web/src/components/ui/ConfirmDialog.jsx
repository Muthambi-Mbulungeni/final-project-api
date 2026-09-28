import { useEffect, useRef } from 'react'

// Confirmation dialog built on the native <dialog> element, which provides
// focus handling, Escape to close and a backdrop.
export default function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = 'Confirm',
  busy = false,
  error,
  onConfirm,
  onCancel,
}) {
  const dialogRef = useRef(null)

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog) return
    if (open && !dialog.open) dialog.showModal()
    if (!open && dialog.open) dialog.close()
  }, [open])

  function handleCancel(event) {
    event.preventDefault()
    if (!busy) onCancel()
  }

  return (
    <dialog
      ref={dialogRef}
      onCancel={handleCancel}
      aria-labelledby="confirm-dialog-title"
      className="m-auto w-full max-w-md rounded-lg border border-stone-200 bg-white p-0 text-stone-900 shadow-md shadow-stone-900/10 backdrop:bg-stone-900/40"
    >
      <div className="p-6">
        <h2 id="confirm-dialog-title" className="text-base font-semibold">
          {title}
        </h2>
        <p className="mt-2 text-sm text-stone-600">{message}</p>
        {error && (
          <p role="alert" className="mt-4 rounded-md border border-brand-200 bg-brand-50 px-3 py-2 text-sm text-brand-800">
            {error}
          </p>
        )}
      </div>

      <div className="flex justify-end gap-2 border-t border-stone-200 bg-stone-50 px-6 py-4">
        <button
          type="button"
          onClick={onCancel}
          disabled={busy}
          className="rounded-md border border-stone-300 bg-white px-3 py-2 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50 disabled:opacity-60"
        >
          Cancel
        </button>
        <button
          type="button"
          onClick={onConfirm}
          disabled={busy}
          className="rounded-md bg-brand-600 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-60"
        >
          {busy ? 'Working…' : confirmLabel}
        </button>
      </div>
    </dialog>
  )
}
