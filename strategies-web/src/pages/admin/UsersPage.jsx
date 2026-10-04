import { useState } from 'react'
import { Link } from 'react-router-dom'
import ConfirmDialog from '../../components/ui/ConfirmDialog'
import DataTable from '../../components/ui/DataTable'
import PageHeader from '../../components/ui/PageHeader'
import StatusBadge from '../../components/ui/StatusBadge'
import { useToast } from '../../components/ui/toastContext'
import { apiPatch } from '../../lib/api'
import { getInitials } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { usersPath } from '../../lib/listPaths'
import { useApi } from '../../lib/useApi'
import { useDebouncedValue } from '../../lib/useDebouncedValue'

const PAGE_SIZE = 10

// GET /api/admin/users returns { data: [{ id, fullName, email, role, isActive }], totalRecords, totalPages, currentPage }
// and supports page, pageSize and search (name or email).
// PATCH /api/admin/users/{id}/suspend and /reinstate change access; both return 409 if already in that state.
// There is deliberately no Delete: the API returns 405 because user records are retained for POPIA and the audit trail.
const ACTIONS = {
  suspend: {
    title: 'Suspend this user?',
    message: (name) =>
      `${name} will not be able to sign in until their account is reinstated. Their records and case history are kept.`,
    confirmLabel: 'Suspend user',
    endpoint: 'suspend',
    isActiveAfter: false,
    fallbackMessage: 'User suspended.',
  },
  reinstate: {
    title: 'Reinstate this user?',
    message: (name) => `${name} will be able to sign in again with their existing password.`,
    confirmLabel: 'Reinstate user',
    endpoint: 'reinstate',
    isActiveAfter: true,
    fallbackMessage: 'User reinstated.',
  },
}

function displayName(user) {
  return user.fullName ?? user.name ?? 'Unnamed user'
}

export default function UsersPage() {
  const toast = useToast()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  // Status changes made on this page, shown immediately without refetching the list.
  const [statusOverrides, setStatusOverrides] = useState({})
  const [target, setTarget] = useState(null)
  const [action, setAction] = useState({ busy: false, error: '' })
  const search = useDebouncedValue(searchInput, 400)

  const request = useApi(usersPath({ page, pageSize: PAGE_SIZE, search }))
  const response = request.data
  const users = listItems(response).map((user) =>
    user.id in statusOverrides ? { ...user, isActive: statusOverrides[user.id] } : user,
  )

  const current = target ? ACTIONS[target.kind] : null

  function handleSearchChange(value) {
    setSearchInput(value)
    setPage(1)
  }

  function openAction(user, kind) {
    setAction({ busy: false, error: '' })
    setTarget({ user, kind })
  }

  function closeDialog() {
    setTarget(null)
    setAction({ busy: false, error: '' })
  }

  async function confirmAction() {
    if (!target) return
    const { user, kind } = target
    const config = ACTIONS[kind]
    setAction({ busy: true, error: '' })

    try {
      const result = await apiPatch(`/api/admin/users/${encodeURIComponent(user.id)}/${config.endpoint}`)
      setStatusOverrides((previous) => ({ ...previous, [user.id]: config.isActiveAfter }))
      toast.show(`${displayName(user)}: ${result?.message ?? config.fallbackMessage}`)
      closeDialog()
    } catch (error) {
      // e.g. 409 "User account is already suspended." when another admin got there first.
      setAction({ busy: false, error: error.message })
    }
  }

  const columns = [
    {
      key: 'fullName',
      header: 'User',
      render: (user) => (
        <div className="flex items-center gap-3">
          <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-stone-100 text-xs font-semibold text-stone-700">
            {getInitials(displayName(user))}
          </span>
          <div className="min-w-0">
            <p className="truncate font-medium text-stone-900">{displayName(user)}</p>
            {user.email && <p className="truncate text-xs text-stone-500">{user.email}</p>}
          </div>
        </div>
      ),
    },
    {
      key: 'role',
      header: 'Role',
      cardInline: true,
      render: (user) => (user.role === undefined || user.role === null ? '—' : <StatusBadge type="userRole" value={user.role} />),
    },
    {
      key: 'isActive',
      header: 'Status',
      cardInline: true,
      render: (user) =>
        user.isActive === false ? (
          <span className="inline-flex items-center gap-1.5 rounded-md bg-brand-50 px-2 py-0.5 text-xs font-medium text-brand-700 ring-1 ring-brand-200 ring-inset">
            <span className="size-1.5 rounded-full bg-current opacity-70" aria-hidden="true" />
            Suspended
          </span>
        ) : (
          <span className="inline-flex items-center gap-1.5 rounded-md bg-emerald-50 px-2 py-0.5 text-xs font-medium text-emerald-800 ring-1 ring-emerald-200 ring-inset">
            <span className="size-1.5 rounded-full bg-current opacity-70" aria-hidden="true" />
            Active
          </span>
        ),
    },
    {
      key: 'actions',
      header: <span className="sr-only">Actions</span>,
      align: 'right',
      render: (user) => {
        if (!user.id) return null
        const name = displayName(user)
        const suspended = user.isActive === false

        return (
          <div className="flex items-center justify-end gap-1">
            {/* The name travels in router state, not the URL, so it never lands in browser history or access logs. */}
            <Link
              to={{ pathname: '/admin/audit-log', search: `?userId=${encodeURIComponent(user.id)}` }}
              state={{ userName: name }}
              aria-label={`Activity for ${name}`}
              className="rounded-md px-2 py-1 text-sm font-medium text-stone-600 transition-colors hover:bg-stone-100 hover:text-stone-900"
            >
              Activity
            </Link>
            {suspended ? (
              <button
                type="button"
                onClick={() => openAction(user, 'reinstate')}
                aria-label={`Reinstate ${name}`}
                className="rounded-md px-2 py-1 text-sm font-medium text-stone-800 transition-colors hover:bg-stone-100 hover:text-stone-900"
              >
                Reinstate
              </button>
            ) : (
              <button
                type="button"
                onClick={() => openAction(user, 'suspend')}
                aria-label={`Suspend ${name}`}
                className="rounded-md px-2 py-1 text-sm font-medium text-brand-700 transition-colors hover:bg-brand-50 hover:text-brand-800"
              >
                Suspend
              </button>
            )}
          </div>
        )
      },
    },
  ]

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Users"
        description="Find accounts by name or email, suspend or reinstate access, and review what each user has changed."
      />

      <div className="mt-8">
        <DataTable
          caption="Users"
          columns={columns}
          rows={users}
          loading={request.loading}
          error={request.error?.message}
          onRetry={request.reload}
          searchPlaceholder="Search by name or email"
          searchValue={searchInput}
          onSearchChange={handleSearchChange}
          pagination={{
            page: response?.currentPage ?? page,
            pageSize: PAGE_SIZE,
            totalCount: response?.totalRecords ?? 0,
            totalPages: response?.totalPages ?? 1,
            onPageChange: setPage,
          }}
          emptyTitle="No users found"
          emptyMessage="Registered accounts appear here."
        />
      </div>

      <ConfirmDialog
        open={target !== null}
        title={current?.title ?? ''}
        message={target && current ? current.message(displayName(target.user)) : ''}
        confirmLabel={current?.confirmLabel}
        busy={action.busy}
        error={action.error}
        onConfirm={confirmAction}
        onCancel={closeDialog}
      />
    </>
  )
}
