import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { ChevronRight, LogOut, Menu, X } from 'lucide-react'
import NotificationBell from './NotificationBell'
import logo from '../../assets/strategies-logo.png'
import { findCurrentItem, NAVIGATION, WORKSPACE_LABELS } from '../../config/navigation'
import { signOut } from '../../lib/auth'
import { getInitials } from '../../lib/format'
import { ROLES } from '../../lib/session'
import { useIdleSignOut } from '../../lib/useIdleSignOut'
import { useSession } from '../../lib/useSession'
import { isBuilt } from '../../routes/modulePages'

const APP_NAME = 'Strategies Migration Services'

export default function AppShell({ role }) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [signingOut, setSigningOut] = useState(false)
  const openButtonRef = useRef(null)
  const closeButtonRef = useRef(null)
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const session = useSession()
  const items = NAVIGATION[role].filter(isBuilt)
  const workspace = WORKSPACE_LABELS[role]
  const current = findCurrentItem(items, pathname)

  useEffect(() => {
    document.title = current ? `${current.label} · ${workspace} · ${APP_NAME}` : APP_NAME
  }, [current, workspace])

  // Mobile drawer: focus moves into it on open; Escape closes it and returns focus to the menu button.
  useEffect(() => {
    if (!menuOpen) return undefined
    closeButtonRef.current?.focus()

    function handleKey(event) {
      if (event.key === 'Escape') {
        setMenuOpen(false)
        openButtonRef.current?.focus()
      }
    }

    document.addEventListener('keydown', handleKey)
    return () => document.removeEventListener('keydown', handleKey)
  }, [menuOpen])

  function closeMenu() {
    setMenuOpen(false)
    openButtonRef.current?.focus()
  }

  async function leave(reason) {
    setSigningOut(true)
    await signOut()
    navigate('/login', { replace: true, state: { reason } })
  }

  // Real staff sessions only: staff screens show many clients' documents.
  // Development sessions are exempt so a demo is never interrupted.
  useIdleSignOut(session?.source === 'api' && role !== 'client', () => leave('idle'))

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[16rem_1fr]">
      {/* Fixed and off-screen until focused, so it never takes a grid cell (WCAG 2.4.1). */}
      <a
        href="#main-content"
        className="fixed top-3 left-3 z-50 -translate-y-24 rounded-md bg-white px-4 py-2 text-sm font-medium text-stone-900 shadow-md focus:translate-y-0"
      >
        Skip to main content
      </a>

      {menuOpen && (
        <div className="fixed inset-0 z-30 bg-stone-900/40 lg:hidden" onClick={closeMenu} aria-hidden="true" />
      )}

      {/* Below lg the closed drawer is invisible as well as off-screen, which removes its links from the
          Tab order. Visibility transitions with transform, so the slide-out still animates. */}
      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-64 flex-col border-r border-stone-200 bg-white transition-[transform,visibility] duration-200 lg:sticky lg:top-0 lg:bottom-auto lg:h-screen lg:translate-x-0 ${
          menuOpen ? 'translate-x-0' : '-translate-x-full max-lg:invisible'
        }`}
      >
        <div className="flex h-16 items-center justify-between border-b border-stone-200 px-5">
          <img src={logo} alt={APP_NAME} className="h-10 w-auto" />
          <button
            ref={closeButtonRef}
            type="button"
            onClick={closeMenu}
            className="rounded-md p-1.5 text-stone-500 hover:bg-stone-100 hover:text-stone-900 lg:hidden"
            aria-label="Close navigation"
          >
            <X className="size-5" strokeWidth={1.75} />
          </button>
        </div>

        <nav className="flex-1 space-y-0.5 overflow-y-auto px-3 pt-5" aria-label="Main">
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
            onClick={() => leave('signed-out')}
            disabled={signingOut}
            className="flex w-full items-center gap-3 rounded-md px-3 py-2 text-sm font-medium text-stone-600 hover:bg-stone-50 hover:text-stone-900 disabled:cursor-wait disabled:opacity-60"
          >
            <LogOut className="size-4 text-stone-400" strokeWidth={1.75} aria-hidden="true" />
            {signingOut ? 'Signing out…' : 'Sign out'}
          </button>
        </div>
      </aside>

      {/* While the drawer is open (mobile only), the page behind it cannot be reached by Tab or a screen reader. */}
      <div className="flex min-w-0 flex-col" inert={menuOpen}>
        <header className="sticky top-0 z-20 flex h-16 items-center gap-4 border-b border-stone-200 bg-white px-4 sm:px-6 lg:px-10">
          <button
            ref={openButtonRef}
            type="button"
            onClick={() => setMenuOpen(true)}
            className="rounded-md p-1.5 text-stone-500 hover:bg-stone-100 hover:text-stone-900 lg:hidden"
            aria-label="Open navigation"
            aria-expanded={menuOpen}
          >
            <Menu className="size-5" strokeWidth={1.75} />
          </button>

          <nav aria-label="Breadcrumb" className="min-w-0 flex-1">
            <ol className="flex items-center gap-1.5 text-sm">
              <li className="hidden truncate text-stone-500 sm:block">{workspace}</li>
              {current && (
                <>
                  <li className="hidden text-stone-300 sm:block" aria-hidden="true">
                    <ChevronRight className="size-4" strokeWidth={1.75} />
                  </li>
                  <li className="truncate font-medium text-stone-900" aria-current="page">
                    {current.label}
                  </li>
                </>
              )}
            </ol>
          </nav>

          <NotificationBell />

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

        {/* tabIndex -1 lets the skip link move focus here; no outline because <main> is not a control. */}
        <main id="main-content" tabIndex={-1} className="flex-1 px-4 py-8 outline-none sm:px-6 lg:px-10">
          <div className="mx-auto w-full max-w-7xl">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  )
}
