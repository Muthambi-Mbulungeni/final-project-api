import { Link } from 'react-router-dom'
import logo from '../../assets/strategies-logo.png'
import JourneyIntro from './JourneyIntro'

function PrivacyLine({ className = '' }) {
  return (
    <p className={`text-center text-xs leading-relaxed text-stone-500 ${className}`}>
      Client and consultant portal. Personal information is processed in accordance with POPIA.{' '}
      <Link to="/privacy" className="font-medium text-stone-600 underline hover:text-stone-900">
        Privacy notice
      </Link>
    </p>
  )
}

// Sign-in and password reset: the journey story beside (desktop) or above (phones) a calm,
// card-free form on white. The form side carries the real logo on desktop, where the dark
// panel has no room for it (the logo file has a white background).
function JourneyLayout({ children, year }) {
  return (
    <div className="min-h-screen bg-white lg:grid lg:grid-cols-[minmax(0,44fr)_minmax(0,56fr)]">
      <JourneyIntro />
      <div className="flex flex-col lg:min-h-screen">
        <main className="flex flex-1 justify-center px-6 pt-6 pb-10 sm:px-8 lg:items-center lg:py-16">
          <div className="w-full max-w-[380px]">
            <img
              src={logo}
              alt="Strategies Migration Services"
              className="mx-auto hidden h-10 w-auto lg:block motion-safe:lg:animate-rise-soft"
              style={{ animationDelay: '0.15s' }}
            />
            <div className="lg:mt-10">{children}</div>
            <PrivacyLine className="mt-10 lg:mt-12" />
          </div>
        </main>
        <footer className="pb-6 text-center text-xs text-stone-500">© {year} Strategies Migration Services</footer>
      </div>
    </div>
  )
}

// Centred card layout shared by the enquiry, privacy and not-found pages.
// `wide` gives pages with more content (like the enquiry) extra room.
// `journey` switches to the sign-in layout above (sign-in and password reset).
export default function AuthLayout({ children, wide = false, journey = false }) {
  const year = new Date().getFullYear()

  if (journey) return <JourneyLayout year={year}>{children}</JourneyLayout>

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

          <PrivacyLine className="mt-6" />
        </div>
      </main>

      <footer className="pb-6 text-center text-xs text-stone-500">© {year} Strategies Migration Services</footer>
    </div>
  )
}
