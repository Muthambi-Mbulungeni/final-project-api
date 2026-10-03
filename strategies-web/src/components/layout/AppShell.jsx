import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { ChevronRight, LogOut, Menu, X } from 'lucide-react'
import IdleWarningDialog from './IdleWarningDialog'
import ErrorBoundary from '../ErrorBoundary'
import ErrorNotice from '../ui/ErrorNotice'
import NotificationBell from './NotificationBell'
import logo from '../../assets/strategies-logo.png'
import { findCurrentItem, NAVIGATION, WORKSPACE_LABELS } from '../../config/navigation'
import { signOut } from '../../lib/auth'
import { getInitials } from '../../lib/format'
import { ROLES } from '../../lib/session'
import { IDLE_WARNING_MS, STAFF_IDLE_TIMEOUT_MS, useIdleSignOut } from '../../lib/useIdleSignOut'
import { useSession } from '../../lib/useSession'
import { isBuilt } from '../../routes/modulePages'

const APP_NAME = 'Strategies Migration Services'

// Development only: VITE_IDLE_TIMEOUT_MS (e.g. 90000) shortens the timeout so the warning can be demonstrated.
// Production builds ignore it and always use the 30-minute policy.
const IDLE_TIMEOUT_MS = (import.meta.env.DEV && Number(import.meta.env.VITE_IDLE_TIMEOUT_MS)) || STAFF_IDLE_TIMEOUT_MS

export default function AppShell({ role }) {
  const [menuOpen, setMenuOpen] = useState(false)
  const [signingOut, setSigningOut] = useState(false)
  const openButtonRef = useRef(null)
  const closeButtonRef = useRef(null)
  const mainRef = useRef(null)
  // Where focus goes when the drawer closes: the menu button (Escape, close, backdrop) or the page (a link).
  const focusAfterCloseRef = useRef(null)
  const navigate = useNavigate()
  const { pathname } = useLocation()
  const session = useSession()
  const items = NAVIGATION[role].filter(isBuilt)
  const workspace = WORKSPACE_LABELS[role]
  const current = findCurrentItem(items, pathname)

  useEffect(() => {
    document.title = current ? `${current.label} · ${workspace} · ${APP_NAME}` : APP_NAME
  }, [current, workspace])

  // Mobile drawer focus management. The page behind the drawer is inert while it is open, and browsers
  // refuse to focus inert elements, so focus can only return once this render has removed inert.
  useEffect(() => {
    if (!menuOpen) {
      const target = focusAfterCloseRef.current
      focusAfterCloseRef.current = null
      target?.current?.focus()
      return undefined
    }

    closeButtonRef.current?.focus()

    function handleKey(event) {
      if (event.key === 'Escape') {
        focusAfterCloseRef.current = openButtonRef
        setMenuOpen(false)
      }
    }

    document.addEventListener('keydown', handleKey)
    return () => document.removeEventListener('keydown', handleKey)
  }, [menuOpen])

  // The drawer only exists below lg. If the viewport widens while it is open (a tablet rotated, a window
  // resized), close it, or the page would stay inert behind what is now the ordinary sidebar.
  useEffect(() => {
    if (!menuOpen) return undefined
    const desktop = window.matchMedia('(min-width: 64rem)')
    function handleWidth(event) {
      if (event.matches) setMenuOpen(false)
    }
    desktop.addEventListener('change', handleWidth)
    return () => desktop.removeEventListener('change', handleWidth)
  }, [menuOpen])

  function closeMenu(returnFocusTo) {
    focusAfterCloseRef.current = returnFocusTo
    setMenuOpen(false)
  }

  async function leave(reason) {
    setSigningOut(true)
    await signOut()
    navigate('/login', { replace: true, state: { reason } })
  }

  // Real staff sessions only: staff screens show many clients' documents.
  // Development sessions are exempt so a demo is never interrupted.
  const { warningEndsAt, extend } = useIdleSignOut(session?.source === 'api' && role !== 'client', () => leave('idle'), {
    timeoutMs: IDLE_TIMEOUT_MS,
  })

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[16rem_1fr]">
      {/* Fixed and off-screen until focused, so it never takes a grid cell (WCAG 2.4.1).
          Inert while the drawer is open: its target is behind the drawer, and Tab must stay in the drawer. */}
      <a
        href="#main-content"
        inert={menuOpen}
        className="fixed top-3 left-3 z-50 -translate-y-24 rounded-md bg-white px-4 py-2 text-sm font-medium text-stone-900 shadow-md focus:translate-y-0"
      >
        Skip to main content
      </a>

      {menuOpen && (
        <div
          className="fixed inset-0 z-30 bg-stone-900/40 lg:hidden"
          onClick={() => closeMenu(openButtonRef)}
          aria-hidden="true"
        />
      )}

      {/* Below lg the closed drawer is invisible as well as off-screen, which removes its links from the
          Tab order. Visibility transitions with transform, so the slide-out still animates. Opening transitions transform only: a hidden element cannot take focus, so visibility must flip at once for the close button to receive it. */}
      <aside
        className={`fixed inset-y-0 left-0 z-40 flex w-64 flex-col border-r border-stone-200 bg-white duration-200 motion-reduce:transition-none lg:sticky lg:top-0 lg:bottom-auto lg:h-screen lg:translate-x-0 ${
          menuOpen ? 'translate-x-0 transition-transform' : '-translate-x-full transition-[transform,visibility] max-lg:invisible'
        }`}
      >
        <div className="flex h-16 items-center justify-between border-b border-stone-200 px-5">
          <img src={logo} alt={APP_NAME} className="h-10 w-auto" />
          <button
            ref={closeButtonRef}
            type="button"
            onClick={() => closeMenu(openButtonRef)}
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
              onClick={() => closeMenu(mainRef)}
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

        {/* tabIndex -1 lets the skip link (and a drawer link) move focus here; no outline because <main> is not a control. */}
        <main
          ref={mainRef}
          id="main-content"
          tabIndex={-1}
          className="flex-1 px-4 py-8 outline-none sm:px-6 lg:px-10"
        >
          <div className="mx-auto w-full max-w-7xl">
            <ErrorBoundary
              resetKey={pathname}
              fallback={({ reset }) => (
                <ErrorNotice
                  message="This page ran into a problem. Try again, or choose another page from the menu."
                  onRetry={reset}
                />
              )}
            >
              <Outlet />
            </ErrorBoundary>
          </div>
        </main>
      </div>

      {/* Outside the inert wrapper, so the warning always works. */}
      <IdleWarningDialog
        endsAt={warningEndsAt}
        warningMs={IDLE_WARNING_MS}
        onStay={extend}
        onSignOut={() => leave('signed-out')}
      />
    </div>
  )
}
