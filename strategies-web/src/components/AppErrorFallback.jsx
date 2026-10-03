import { useEffect } from 'react'

// Last-resort screen when the whole app fails to render. It sits outside the router, so it uses a
// full reload and a plain link rather than router navigation, which may be what failed.
export default function AppErrorFallback() {
  useEffect(() => {
    document.title = 'Something went wrong · Strategies Migration Services'
  }, [])

  return (
    <div className="flex min-h-screen flex-col items-center justify-center px-6 text-center">
      <p className="text-sm font-semibold text-brand-700">Something went wrong</p>
      <h1 className="mt-2 text-2xl font-semibold tracking-tight text-stone-900">This page could not be displayed</h1>
      <p className="mt-2 max-w-md text-sm text-stone-600">
        Reload the page to try again, or return to the portal. If this keeps happening, please contact Strategies
        Migration Services.
      </p>
      <div className="mt-6 flex items-center gap-5">
        <button
          type="button"
          onClick={() => window.location.reload()}
          className="rounded-md bg-brand-600 px-4 py-2 text-sm font-semibold text-white transition-colors hover:bg-brand-700"
        >
          Reload page
        </button>
        <a href="/" className="text-sm font-semibold text-brand-700 hover:text-brand-800">
          Return to the portal
        </a>
      </div>
    </div>
  )
}
