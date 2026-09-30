import { useState } from 'react'
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
// and supports page, pageSize and search (name or email). Suspending uses PATCH /api/admin/users/{id}/suspend.

function displayName(user) {
  return user.fullName ?? user.name ?? 'Unnamed user'
}

export default function UsersPage() {
  const toast = useToast()
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [suspendedIds, setSuspendedIds] = useState(() => new Set())
  const [target, setTarget] = useState(null)
  const [action, setAction] = useState({ busy: false, error: '' })
  const search = useDebouncedValue(searchInput, 400)

  const request = useApi(usersPath({ page, pageSize: PAGE_SIZE, search }))
  const response = request.data
  const users = listItems(response).map((user) =>
    suspendedIds.has(user.id) ? { ...user, isActive: false } : user,
  )

  function handleSearchChange(value) {
    setSearchInput(value)
    setPage(1)
  }

  function openSuspend(user) {
    setAction({ busy: false, error: '' })
    setTarget(user)
  }

  function closeDialog() {
    setTarget(null)
    setAction({ busy: false, error: '' })
  }

  async function confirmSuspend() {
    if (!target) return
    setAction({ busy: true, error: '' })

    try {
      const result = await apiPatch(`/api/admin/users/${encodeURIComponent(target.id)}/suspend`)
      setSuspendedIds((previous) => new Set([...previous, target.id]))
      toast.show(`${displayName(target)}: ${result?.message ?? 'User suspended.'}`)
      closeDialog()
    } catch (error) {
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
      render: (user) => (user.role === undefined || user.role === null ? '—' : <StatusBadge type="userRole" value={user.role} />),
    },
    {
      key: 'isActive',
      header: 'Status',
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
      render: (user) =>
        user.isActive === false ? (
          <span className="text-xs text-stone-400">—</span>
        ) : (
          <button
            type="button"
            onClick={() => openSuspend(user)}
            className="rounded-md px-2 py-1 text-sm font-medium text-brand-700 transition-colors hover:bg-brand-50 hover:text-brand-800"
          >
            Suspend
          </button>
        ),
    },
  ]

  return (
    <>
      <PageHeader
        eyebrow="Administration"
        title="Users"
        description="Find accounts by name or email, and suspend access."
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
        title="Suspend this user?"
        message={
          target
            ? `${displayName(target)} will no longer be able to sign in until their account is reactivated.`
            : ''
        }
        confirmLabel="Suspend user"
        busy={action.busy}
        error={action.error}
        onConfirm={confirmSuspend}
        onCancel={closeDialog}
      />
    </>
  )
}
