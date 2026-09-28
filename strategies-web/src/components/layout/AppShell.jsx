import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Bell, LogOut, Menu, X } from 'lucide-react'
import logo from '../../assets/strategies-logo.png'
import { NAVIGATION } from '../../config/navigation'
import { endSession, getSession, ROLES } from '../../lib/session'

function getInitials(name = '') {
  return name
    .split(' ')
    .filter(Boolean)
    .map((part) => part[0])
    .slice(0, 2)
    .join('')
    .toUpperCase()
}

export default function AppShell({ role }) {
  const [menuOpen, setMenuOpen] = useState(false)
  const navigate = useNavigate()
  const session = getSession()
  const items = NAVIGATION[role]

  function handleSignOut() {
    endSession()
    navigate('/login', { replace: true })
  }

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[16rem_1fr]">
      {menuOpen && (
        <div
          className="fixed inset-0 z-30 bg-stone-900/40 lg:hidden"
          onClick={() => setMenuOpen(false)}
          aria-hidden="true"
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-64 flex-col border-r border-stone-200 bg-white transition-transform duration-200 lg:sticky lg:top-0 lg:bottom-auto lg:h-screen lg:translate-x-0 ${
          menuOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        <div className="flex h-16 items-center justify-between border-b border-stone-200 px-5">
          <img src={logo} alt="Strategies Migration Services" className="h-10 w-auto" />
          <button
            type="button"
            onClick={() => setMenuOpen(false)}
            className="rounded-md p-1.5 text-stone-500 hover:bg-stone-100 hover:text-stone-900 lg:hidden"
            aria-label="Close navigation"
          >
            <X className="size-5" strokeWidth={1.75} />
          </button>
        </div>

        <p className="px-5 pt-6 pb-2 text-[11px] font-semibold uppercase tracking-[0.08em] text-stone-500">
          {ROLES[role].label} workspace
        </p>

        <nav className="flex-1 space-y-0.5 overflow-y-auto px-3" aria-label="Main">
          {items.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              onClick={() => setMenuOpen(false)}
              className={({ isActive }) =>
                `group relative flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-stone-100 text-stone-900'
                    : 'text-stone-600 hover:bg-stone-50 hover:text-stone-900'
                }`
              }
            >
              {({ isActive }) => (
                <>
                  {isActive && (
                    <span className="absolute inset-y-1.5 left-0 w-0.5 rounded-full bg-brand-600" aria-hidden="true" />
                  )}
                  <Icon
                    className={`size-4 shrink-0 ${
                      isActive ? 'text-brand-600' : 'text-stone-400 group-hover:text-stone-500'
                    }`}
                    strokeWidth={1.75}
                    aria-hidden="true"
                  />
                  {label}
                </>
              )}
            </NavLink>
          ))}
        </nav>

        <div className="border-t border-stone-200 p-3">
          <button
            type="button"
            onClick={handleSignOut}
            className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-stone-600 hover:bg-stone-50 hover:text-stone-900"
          >
            <LogOut className="size-4 text-stone-400" strokeWidth={1.75} aria-hidden="true" />
            Sign out
          </button>
        </div>
      </aside>

      <div className="flex min-w-0 flex-col">
        <header className="sticky top-0 z-20 flex h-16 items-center gap-4 border-b border-stone-200 bg-white px-4 sm:px-6 lg:px-8">
          <button
            type="button"
            onClick={() => setMenuOpen(true)}
            className="rounded-md p-1.5 text-stone-500 hover:bg-stone-100 hover:text-stone-900 lg:hidden"
            aria-label="Open navigation"
          >
            <Menu className="size-5" strokeWidth={1.75} />
          </button>

          <div className="flex-1" />

          <button
            type="button"
            className="rounded-md p-2 text-stone-500 hover:bg-stone-100 hover:text-stone-900"
            aria-label="Notifications"
          >
            <Bell className="size-5" strokeWidth={1.75} />
          </button>

          <div className="flex items-center gap-3 border-l border-stone-200 pl-4">
            <div className="hidden text-right sm:block">
              <p className="text-sm font-medium text-stone-900">{session?.name}</p>
              <p className="text-xs text-stone-500">{ROLES[role].label}</p>
            </div>
            <span className="flex size-9 items-center justify-center rounded-full bg-stone-900 text-xs font-semibold text-white">
              {getInitials(session?.name)}
            </span>
          </div>
        </header>

        <main className="flex-1 px-4 py-8 sm:px-6 lg:px-8">
          <div className="max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
