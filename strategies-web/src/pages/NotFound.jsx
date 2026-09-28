import { Link } from 'react-router-dom'

export default function NotFound() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center px-6 text-center">
      <p className="text-sm font-semibold text-brand-700">404</p>
      <h1 className="mt-2 text-2xl font-semibold tracking-tight text-stone-900">Page not found</h1>
      <p className="mt-2 text-sm text-stone-600">The page you are looking for does not exist or has moved.</p>
      <Link to="/" className="mt-6 text-sm font-semibold text-brand-700 hover:text-brand-800">
        Return to the portal
      </Link>
    </div>
  )
}
