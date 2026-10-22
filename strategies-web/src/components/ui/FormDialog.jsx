import { useEffect, useId, useRef } from 'react'

// A small form in a dialog: the same native <dialog>, sizes and footer as ConfirmDialog, with fields
// as children. showModal() makes the rest of the page inert, so Tab stays inside the dialog, and the
// browser focuses the first field. Escape and Cancel both call onCancel (ignored while busy).
// Focus goes back to whatever opened the dialog when it closes, including when it is unmounted.
// `error` is a message or a list of messages from the server, shown above the footer.
export default function FormDialog({
  open,
  title,
  description,
  submitLabel = 'Save',
  busyLabel = 'Saving…',
  cancelLabel = 'Cancel',
  busy = false,
  submitDisabled = false,
  error,
  onSubmit,
  onCancel,
  children,
}) {
  const dialogRef = useRef(null)
  const titleId = useId()
  const descriptionId = useId()
  const errors = (Array.isArray(error) ? error : [error]).filter(Boolean)

  useEffect(() => {
    const dialog = dialogRef.current
    if (!dialog || !open) return undefined
    const opener = document.activeElement
    if (!dialog.open) dialog.showModal()

    return () => {
      if (dialog.open) dialog.close()
      if (opener instanceof HTMLElement && opener.isConnected) opener.focus()
    }
  }, [open])

  function handleCancel(event) {
    event.preventDefault()
    if (!busy) onCancel()
  }

  function handleSubmit(event) {
    event.preventDefault()
    if (!busy && !submitDisabled) onSubmit()
  }

  return (
    <dialog
      ref={dialogRef}
      onCancel={handleCancel}
      aria-labelledby={titleId}
      aria-describedby={description ? descriptionId : undefined}
      className="m-auto w-[calc(100%-2rem)] max-w-md rounded-lg border border-stone-200 bg-white p-0 text-stone-900 shadow-md shadow-stone-900/10 backdrop:bg-stone-900/40"
    >
      <form noValidate onSubmit={handleSubmit}>
        <div className="p-5 sm:p-6">
          <h2 id={titleId} className="text-base font-semibold">
            {title}
          </h2>
          {description && (
            <p id={descriptionId} className="mt-2 text-sm text-stone-600">
              {description}
            </p>
          )}
          <div className="mt-5 space-y-4">{children}</div>
          {errors.length > 0 && (
            <div role="alert" className="mt-4 space-y-1 rounded-md border border-brand-200 bg-brand-50 px-3 py-2 text-sm text-brand-800">
              {errors.map((message) => (
                <p key={message}>{message}</p>
              ))}
            </div>
          )}
        </div>

        <div className="grid grid-cols-2 gap-2 rounded-b-lg border-t border-stone-200 bg-stone-50 px-5 py-4 sm:flex sm:justify-end sm:px-6">
          <button
            type="button"
            onClick={onCancel}
            disabled={busy}
            className="rounded-md border border-stone-300 bg-white px-3 py-2 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-50 disabled:opacity-60"
          >
            {cancelLabel}
          </button>
          <button
            type="submit"
            disabled={busy || submitDisabled}
            className={`rounded-md bg-brand-600 px-3 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:opacity-60 ${
              busy ? 'disabled:cursor-wait' : 'disabled:cursor-not-allowed'
            }`}
          >
            {busy ? busyLabel : submitLabel}
          </button>
        </div>
      </form>
    </dialog>
  )
}
