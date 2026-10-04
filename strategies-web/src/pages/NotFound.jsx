import { Link } from 'react-router-dom'
import AuthLayout from '../components/layout/AuthLayout'
import { useDocumentTitle } from '../lib/useDocumentTitle'

// Same branded card as sign-in, so a mistyped link still looks like the practice's portal.
// "/" sends signed-in people to their workspace and everyone else to sign-in.
export default function NotFound() {
  useDocumentTitle('Page not found')

  return (
    <AuthLayout>
      <div className="text-center">
        <p className="text-xs font-semibold uppercase tracking-[0.08em] text-brand-700">Error 404</p>
        <h1 className="mt-2 text-xl font-semibold tracking-tight text-stone-900">Page not found</h1>
        <p className="mt-2 text-sm text-stone-600">
          The page you are looking for does not exist or has moved. Check the address, or return to the portal.
        </p>
        <Link
          to="/"
          className="mt-8 flex w-full items-center justify-center rounded-md bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700"
        >
          Return to the portal
        </Link>
        <p className="mt-6 border-t border-stone-200 pt-6 text-sm text-stone-600">
          New client?{' '}
          <Link to="/enquire" className="font-semibold text-brand-700 hover:text-brand-800">
            Start an enquiry
          </Link>
        </p>
      </div>
    </AuthLayout>
  )
}
