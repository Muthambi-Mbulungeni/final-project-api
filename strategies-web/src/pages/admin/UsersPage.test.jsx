// @vitest-environment jsdom
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { deferred, json } from '../../test/dom'
import ToastProvider from '../../components/ui/ToastProvider'
import { endSession, startSession } from '../../lib/session'
import UsersPage from './UsersPage'

const ME = { id: 'admin-1', fullName: 'System Admin', email: 'admin@nexus.com', role: 'Admin', isActive: true }
const SIPHO = { id: 'cons-1', fullName: 'Sipho Ndlovu', email: 'sipho@nexus.com', role: 'Consultant', isActive: true }

// Routes fetch calls: GET returns the user list, PATCH goes to `patch`.
function stubApi(patch = async (body) => json({ ...SIPHO, ...body })) {
  const fetchMock = vi.fn(async (url, options = {}) => {
    if (options.method === 'PATCH') return patch(JSON.parse(options.body), url)
    return json({ totalRecords: 2, totalPages: 1, currentPage: 1, data: [ME, SIPHO] })
  })
  vi.stubGlobal('fetch', fetchMock)
  return fetchMock
}

function calls(fetchMock, method) {
  return fetchMock.mock.calls.filter(([, options]) => (options?.method ?? 'GET') === method)
}

function renderPage() {
  return render(
    <MemoryRouter>
      <ToastProvider>
        <UsersPage />
      </ToastProvider>
    </MemoryRouter>,
  )
}

// Opens the dialog the way a person does: focus on the Edit button, then activate it.
async function openEdit(name) {
  const button = (await screen.findAllByRole('button', { name: `Edit ${name}` }))[0]
  button.focus()
  fireEvent.click(button)
  const dialog = within(screen.getByRole('dialog', { name: 'Edit user' }))
  // The browser moves focus into a modal dialog; the jsdom stand-in does not, so do it here.
  dialog.getByLabelText('Full name').focus()
  return { button, dialog }
}

function field(dialog, label) {
  return dialog.getByLabelText(label)
}

function saveButton(dialog) {
  return dialog.getByRole('button', { name: 'Save changes' })
}

describe('UsersPage: edit user', () => {
  beforeEach(() => {
    endSession()
    startSession({ accessToken: 'token', refreshToken: 'refresh', role: 'Admin', userId: ME.id, displayName: ME.fullName })
  })

  it('opens the dialog with the user’s current values', async () => {
    stubApi()
    renderPage()
    const { dialog } = await openEdit('Sipho Ndlovu')

    expect(field(dialog, 'Full name').value).toBe('Sipho Ndlovu')
    expect(field(dialog, 'Email address').value).toBe('sipho@nexus.com')
    expect(field(dialog, 'Role').value).toBe('Consultant')
    expect(field(dialog, 'Role').disabled).toBe(false)
  })

  it('cannot save an unchanged form, including one with only spaces added', async () => {
    const fetchMock = stubApi()
    renderPage()
    const { dialog } = await openEdit('Sipho Ndlovu')

    expect(saveButton(dialog).disabled).toBe(true)
    fireEvent.change(field(dialog, 'Full name'), { target: { value: ' Sipho Ndlovu  ' } })
    expect(saveButton(dialog).disabled).toBe(true)
    fireEvent.submit(saveButton(dialog).closest('form'))
    expect(calls(fetchMock, 'PATCH')).toHaveLength(0)

    fireEvent.change(field(dialog, 'Full name'), { target: { value: 'Sipho M. Ndlovu' } })
    expect(saveButton(dialog).disabled).toBe(false)
  })

  it('sends only the changed fields, then updates the row in place and returns focus', async () => {
    const fetchMock = stubApi()
    renderPage()
    const { button, dialog } = await openEdit('Sipho Ndlovu')

    fireEvent.change(field(dialog, 'Email address'), { target: { value: ' sipho.n@nexus.com ' } })
    fireEvent.change(field(dialog, 'Role'), { target: { value: 'Admin' } })
    fireEvent.click(saveButton(dialog))

    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Edit user' })).toBeNull())
    const [url, options] = calls(fetchMock, 'PATCH')[0]
    expect(url).toBe('/api/admin/users/cons-1')
    expect(JSON.parse(options.body)).toEqual({ email: 'sipho.n@nexus.com', role: 'Admin' })

    expect(screen.getAllByText('sipho.n@nexus.com').length).toBeGreaterThan(0)
    expect(screen.queryByText('sipho@nexus.com')).toBeNull()
    expect(calls(fetchMock, 'GET')).toHaveLength(1)
    expect(screen.getByText('Sipho Ndlovu: changes saved.')).toBeTruthy()
    expect(document.activeElement).toBe(button)
  })

  it('disables the buttons while saving', async () => {
    const pending = deferred()
    stubApi(() => pending.promise)
    renderPage()
    const { dialog } = await openEdit('Sipho Ndlovu')

    fireEvent.change(field(dialog, 'Full name'), { target: { value: 'Sipho M. Ndlovu' } })
    fireEvent.click(saveButton(dialog))

    expect((await dialog.findByRole('button', { name: 'Saving…' })).disabled).toBe(true)
    expect(dialog.getByRole('button', { name: 'Cancel' }).disabled).toBe(true)
    pending.resolve(json({ ...SIPHO, fullName: 'Sipho M. Ndlovu' }))
    await waitFor(() => expect(screen.queryByRole('dialog', { name: 'Edit user' })).toBeNull())
  })

  it('checks the fields before sending', async () => {
    const fetchMock = stubApi()
    renderPage()
    const { dialog } = await openEdit('Sipho Ndlovu')

    fireEvent.change(field(dialog, 'Full name'), { target: { value: '   ' } })
    fireEvent.change(field(dialog, 'Email address'), { target: { value: 'sipho@nexus' } })
    fireEvent.click(saveButton(dialog))

    expect(dialog.getByText('Enter the user’s full name.')).toBeTruthy()
    expect(dialog.getByText('Enter a valid email address, like name@example.com.')).toBeTruthy()
    expect(document.activeElement).toBe(field(dialog, 'Full name'))
    expect(calls(fetchMock, 'PATCH')).toHaveLength(0)
  })

  it('shows 400 errors from the API in the dialog', async () => {
    stubApi(async () => json({ message: 'Validation failed.', errors: ['Full name must be 100 characters or fewer.'] }, 400))
    renderPage()
    const { dialog } = await openEdit('Sipho Ndlovu')

    fireEvent.change(field(dialog, 'Full name'), { target: { value: 'A much longer name' } })
    fireEvent.click(saveButton(dialog))

    expect((await dialog.findByRole('alert')).textContent).toBe('Full name must be 100 characters or fewer.')
    expect(saveButton(dialog).disabled).toBe(false)
  })

  it('shows a duplicate email (409) under the email field', async () => {
    stubApi(async () => json({ message: 'Another account already uses this email address.' }, 409))
    renderPage()
    const { dialog } = await openEdit('Sipho Ndlovu')

    fireEvent.change(field(dialog, 'Email address'), { target: { value: 'admin@nexus.com' } })
    fireEvent.click(saveButton(dialog))

    const error = await dialog.findByText('Another account already uses this email address.')
    const email = field(dialog, 'Email address')
    expect(email.getAttribute('aria-describedby')).toBe(error.id)
    expect(email.getAttribute('aria-invalid')).toBe('true')
    expect(dialog.queryByRole('alert')).toBeNull()
  })

  it('does not let administrators change their own role', async () => {
    const fetchMock = stubApi(async (body) => json({ ...ME, ...body }))
    renderPage()
    const { dialog } = await openEdit('System Admin')

    const role = field(dialog, 'Role')
    expect(role.disabled).toBe(true)
    expect(role.getAttribute('aria-describedby')).toBe('edit-user-role-hint')
    expect(dialog.getByText(/You cannot change your own role/)).toBeTruthy()

    fireEvent.change(field(dialog, 'Full name'), { target: { value: 'System Administrator' } })
    fireEvent.click(saveButton(dialog))
    await waitFor(() => expect(calls(fetchMock, 'PATCH')).toHaveLength(1))
    expect(JSON.parse(calls(fetchMock, 'PATCH')[0][1].body)).toEqual({ fullName: 'System Administrator' })
  })

  it('closes on Escape and Cancel without saving, returning focus to Edit', async () => {
    const fetchMock = stubApi()
    renderPage()

    let { button, dialog } = await openEdit('Sipho Ndlovu')
    fireEvent.change(field(dialog, 'Full name'), { target: { value: 'Changed' } })
    fireEvent(screen.getByRole('dialog', { name: 'Edit user' }), new Event('cancel', { cancelable: true }))
    expect(screen.queryByRole('dialog', { name: 'Edit user' })).toBeNull()
    expect(document.activeElement).toBe(button)

    ;({ button, dialog } = await openEdit('Sipho Ndlovu'))
    expect(field(dialog, 'Full name').value).toBe('Sipho Ndlovu')
    fireEvent.click(dialog.getByRole('button', { name: 'Cancel' }))
    expect(screen.queryByRole('dialog', { name: 'Edit user' })).toBeNull()
    expect(document.activeElement).toBe(button)
    expect(calls(fetchMock, 'PATCH')).toHaveLength(0)
  })
})
