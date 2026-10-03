// Shown for a moment on page load while the portal checks for a signed-in session,
// so people who are still signed in never see the sign-in form flash up.
export default function SessionCheck() {
  return (
    <div className="flex min-h-screen items-center justify-center bg-stone-50" role="status" aria-live="polite">
      <div className="flex items-center gap-3 text-sm text-stone-500">
        <span
          className="size-4 animate-spin rounded-full border-2 border-stone-300 border-t-brand-600"
          aria-hidden="true"
        />
        Checking your session…
      </div>
    </div>
  )
}
