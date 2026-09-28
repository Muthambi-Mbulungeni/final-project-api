import { useState } from 'react'
import ConfirmDialog from '../../components/ui/ConfirmDialog'
import DataTable from '../../components/ui/DataTable'
import PageHeader from '../../components/ui/PageHeader'
import StatusBadge from '../../components/ui/StatusBadge'
import { apiPatch } from '../../lib/api'
import { getInitials } from '../../lib/format'
import { listItems } from '../../lib/listData'
import { useApi } from '../../lib/useApi'

const PAGE_SIZE = 10

// Matches GET /api/admin/users in frontend-contracts.md:
// { data: [{ id, fullName, email, role, isActive }], totalRecords, totalPages, currentPage }.
// Supports page and pageSize. Suspending uses PATCH /api/admin/users/{id}/suspend.
function buildUsersPath(page) {
  const params = new URLSearchParams({ page: String(page), pageSize: String(PAGE_SIZE) })
  return `/api/admin/users?${params.toString()}`
}

function displayName(user) {
  return user.fullName ?? user.name ?? 'Unnamed user'
}

export default function UsersPage() {
  const [page, setPage] = useState(1)
  const [suspendedIds, setSuspendedIds] = useState(() => new Set())
  const [target, setTarget] = useState(null)
  const [action, setAction] = useState({ busy: false, error: '' })
  const [notice, setNotice] = useState('')

  const request = useApi(buildUsersPath(page))
  const response = request.data
  const users = listItems(response).map((user) =>
    suspendedIds.has(user.id) ? { ...user, isActive: false } : user,
  )

  function openSuspend(user) {
    setNotice('')
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
      setNotice(`${displayName(target)}: ${result?.message ?? 'User suspended.'}`)
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
        description="View accounts and suspend access."
        actions={
          <span className="inline-flex items-center rounded-md border border-dashed border-stone-300 px-2.5 py-1 text-xs font-medium text-stone-500">
            Mock API
          </span>
        }
      />

      {notice && (
        <div
          role="status"
          className="mt-8 flex items-center justify-between gap-4 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3"
        >
          <p className="text-sm text-emerald-900">{notice}</p>
          <button
            type="button"
            onClick={() => setNotice('')}
            className="shrink-0 text-sm font-medium text-emerald-900 hover:underline"
          >
            Dismiss
          </button>
        </div>
      )}

      <div className="mt-8">
        <DataTable
          caption="Users"
          columns={columns}
          rows={users}
          loading={request.loading}
          error={request.error?.message}
          onRetry={request.reload}
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
