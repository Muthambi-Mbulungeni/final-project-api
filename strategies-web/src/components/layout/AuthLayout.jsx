import { Link } from 'react-router-dom'
import logo from '../../assets/strategies-logo.png'

// Centred card layout shared by the sign-in, password reset, enquiry and privacy pages.
// `wide` gives pages with more content (like the enquiry) extra room.
export default function AuthLayout({ children, wide = false }) {
  const year = new Date().getFullYear()

  return (
    <div className="flex min-h-screen flex-col bg-stone-50">
      <main className="flex flex-1 items-center justify-center px-4 py-12 sm:px-6">
        <div className={`w-full ${wide ? 'max-w-xl' : 'max-w-md'}`}>
          <div className="overflow-hidden rounded-xl border border-stone-200 bg-white shadow-sm shadow-stone-900/5">
            <div className="h-1 bg-brand-600" aria-hidden="true" />
            <div className="px-6 py-10 sm:px-10">
              <img src={logo} alt="Strategies Migration Services" className="mx-auto h-12 w-auto" />
              <div className="mt-8">{children}</div>
            </div>
          </div>

          <p className="mt-6 text-center text-xs leading-relaxed text-stone-500">
            Client and consultant portal. Personal information is processed in accordance with POPIA.{' '}
            <Link to="/privacy" className="font-medium text-stone-600 underline hover:text-stone-900">
              Privacy notice
            </Link>
          </p>
        </div>
      </main>

      <footer className="pb-6 text-center text-xs text-stone-500">© {year} Strategies Migration Services</footer>
    </div>
  )
}
