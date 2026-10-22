// @vitest-environment jsdom
import { useEffect, useRef, useState } from 'react'
import { fireEvent, render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import '../../test/dom'
import ToastProvider from './ToastProvider'
import { useToast } from './toastContext'

// Opens with showModal() and closes in its cleanup, like FormDialog: by then React has already
// taken the element out of the page.
function Panel() {
  const ref = useRef(null)
  useEffect(() => {
    const dialog = ref.current
    dialog.showModal()
    return () => {
      if (dialog.open) dialog.close()
    }
  }, [])
  return <dialog ref={ref}>Panel</dialog>
}

function Page() {
  const toast = useToast()
  const [open, setOpen] = useState(true)
  return (
    <>
      {open && <Panel />}
      <button
        type="button"
        onClick={() => {
          setOpen(false)
          toast.show('Changes saved.')
        }}
      >
        Save
      </button>
    </>
  )
}

describe('ToastProvider', () => {
  it('shows a toast sent while the open dialog it was inside unmounts', async () => {
    render(
      <ToastProvider>
        <Page />
      </ToastProvider>,
    )
    expect(document.querySelector('dialog[open]')).not.toBeNull()

    fireEvent.click(screen.getByRole('button', { name: 'Save' }))

    const toast = await screen.findByText('Changes saved.')
    expect(toast.isConnected).toBe(true)
    expect(document.querySelector('dialog')).toBeNull()
  })
})
