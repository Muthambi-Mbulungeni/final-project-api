import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { ROLES, startDevSession } from '../../lib/session'

// Development-only shortcut to open each role's workspace before real sign-in exists.
// Vite sets import.meta.env.DEV to false in production builds, so this never ships.
export default function DevRoleSwitcher() {
  const [open, setOpen] = useState(false)
  const navigate = useNavigate()

  if (!import.meta.env.DEV) return null

  function enterAs(role) {
    startDevSession(role)
    setOpen(false)
    navigate(ROLES[role].home, { replace: true })
  }

  return (
    <div className="fixed bottom-20 left-4 z-50 flex flex-col items-start gap-2">
      {open && (
        <div role="menu" className="w-52 rounded-lg border border-stone-200 bg-white p-1.5 shadow-md shadow-stone-900/10">
          <p className="px-2.5 pt-1.5 pb-1 text-[11px] font-semibold uppercase tracking-[0.08em] text-stone-500">
            Open workspace as
          </p>
          {Object.values(ROLES).map((role) => (
            <button
              key={role.key}
              type="button"
              role="menuitem"
              onClick={() => enterAs(role.key)}
              className="block w-full rounded-md px-2.5 py-1.5 text-left text-sm text-stone-800 hover:bg-stone-100"
            >
              {role.label}
            </button>
          ))}
        </div>
      )}
      <button
        type="button"
        onClick={() => setOpen((current) => !current)}
        aria-expanded={open}
        aria-haspopup="menu"
        className="rounded-full bg-stone-900 px-3 py-1.5 text-xs font-semibold text-white shadow-sm hover:bg-stone-800"
      >
        Dev
      </button>
    </div>
  )
}
