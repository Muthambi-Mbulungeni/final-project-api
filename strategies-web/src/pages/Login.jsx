import { useState } from 'react'
import { Link, Navigate, useLocation } from 'react-router-dom'
import { ArrowBigUpDash, CircleCheck, Clock, Eye, EyeOff, LoaderCircle } from 'lucide-react'
import AuthLayout from '../components/layout/AuthLayout'
import { refinedInputClasses } from '../components/ui/fieldStyles'
import TextField from '../components/ui/TextField'
import { destinationFor, signIn, signInNotice } from '../lib/auth'
import { signInErrorMessage } from '../lib/authMessages'
import { useDocumentTitle } from '../lib/useDocumentTitle'
import { useResendConfirmation } from '../lib/useResendConfirmation'
import { useSession } from '../lib/useSession'
import { validateSignIn } from '../lib/validation'

// Field ids in on-screen order, used to move focus to the first invalid field.
const FIELD_ORDER = ['email', 'password']

export default function Login() {
  useDocumentTitle('Sign in')
  const location = useLocation()
  const session = useSession()
  const [values, setValues] = useState({ email: '', password: '' })
  const [errors, setErrors] = useState({})
  const [formError, setFormError] = useState('')
  // A plain 401 may be an unconfirmed account (the API does not say which), so a resend is offered.
  const [offerResend, setOfferResend] = useState(false)
  const resend = useResendConfirmation()
  const [submitting, setSubmitting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [capsLockOn, setCapsLockOn] = useState(false)
  // A short shake when the API refuses the sign-in (motion-safe only), like a desktop login screen.
  const [shaking, setShaking] = useState(false)

  const from = location.state?.from
  const reason = location.state?.reason
  const notice = signInNotice(reason)
  const positiveNotice = reason === 'signed-out' || reason === 'password-reset' || reason === 'email-confirmed'
  const NoticeIcon = positiveNotice ? CircleCheck : Clock

  function handleChange(event) {
    const { name, value } = event.target
    setValues((previous) => ({ ...previous, [name]: value }))
    setFormError('')
    setOfferResend(false)
    resend.reset()
    if (errors[name]) {
      setErrors((previous) => ({ ...previous, [name]: undefined }))
    }
  }

  function handlePasswordKey(event) {
    setCapsLockOn(Boolean(event.getModifierState?.('CapsLock')))
  }

  async function handleSubmit(event) {
    event.preventDefault()
    const found = validateSignIn(values)
    setErrors(found)

    const firstInvalid = FIELD_ORDER.find((field) => found[field])
    if (firstInvalid) {
      // Keyboard and screen-reader users land on the problem, and hear its error via aria-describedby.
      document.getElementById(firstInvalid)?.focus()
      return
    }

    setSubmitting(true)
    setFormError('')
    setOfferResend(false)
    resend.reset()

    try {
      // On success the session changes and this page redirects below.
      await signIn(values.email.trim(), values.password)
    } catch (error) {
      setFormError(signInErrorMessage(error))
      setOfferResend(error?.status === 401 && !error?.code)
      setShaking(true)
      setSubmitting(false)
    }
  }

  // Signed in (just now, or already): go to the page they wanted, inside their own workspace.
  if (session) {
    return <Navigate to={destinationFor(session, from)} replace />
  }

  const passwordErrorId = 'password-error'

  // Staggered entrance for the form; motion-safe only.
  const rise = (seconds) => ({ animationDelay: `${seconds}s` })

  return (
    <AuthLayout journey>
      <h1
        className="text-center text-[26px] font-semibold tracking-[-0.022em] text-stone-900 motion-safe:animate-rise-soft lg:text-[30px]"
        style={rise(0.22)}
      >
        Welcome back
      </h1>
      <p className="mt-1.5 text-center text-[15px] text-stone-500 motion-safe:animate-rise-soft" style={rise(0.27)}>
        Sign in to your client and staff portal.
      </p>

      {notice && !formError && (
        <p
          role="status"
          className="mx-auto mt-5 flex w-fit max-w-full items-start gap-2 rounded-2xl bg-stone-100 px-3.5 py-2 text-[13px] text-stone-700 motion-safe:animate-rise-soft"
          style={rise(0.35)}
        >
          <NoticeIcon
            className={`mt-px size-4 shrink-0 ${positiveNotice ? 'text-emerald-600' : 'text-stone-500'}`}
            strokeWidth={2}
            aria-hidden="true"
          />
          {notice}
        </p>
      )}

      <form
        noValidate
        onSubmit={handleSubmit}
        onAnimationEnd={(event) => event.target === event.currentTarget && setShaking(false)}
        className={`mt-7 space-y-5 ${shaking ? 'motion-safe:animate-shake' : ''}`}
      >
        {formError && (
          <div role="alert" className="rounded-xl bg-brand-50 px-3.5 py-2.5 text-sm text-brand-800">
            {formError}
          </div>
        )}

        {offerResend && (
          <div className="-mt-2 space-y-2">
            <button
              type="button"
              onClick={() => resend.send(values.email)}
              disabled={resend.sending}
              className="inline-flex items-center gap-1.5 text-[13px] font-medium text-brand-700 hover:text-brand-800 disabled:cursor-wait disabled:opacity-80"
            >
              {resend.sending && <LoaderCircle className="size-4 motion-safe:animate-spin" strokeWidth={2.25} aria-hidden="true" />}
              {resend.sending ? 'Sending…' : 'Resend confirmation email'}
            </button>
            {/* Always rendered so the outcome is announced when it appears. */}
            <div role="status">
              {resend.message && <p className="text-[13px] text-stone-700">{resend.message}</p>}
            </div>
            {resend.error && (
              <p role="alert" className="text-[13px] text-brand-700">
                {resend.error}
              </p>
            )}
          </div>
        )}

        <div className="motion-safe:animate-rise-soft" style={rise(0.34)}>
          <TextField
            id="email"
            label="Email address"
            type="email"
            autoComplete="email"
            value={values.email}
            onChange={handleChange}
            error={errors.email}
            refined
          />
        </div>

        <div className="motion-safe:animate-rise-soft" style={rise(0.4)}>
          <div className="flex items-center justify-between gap-4">
            <label htmlFor="password" className="block text-[13px] font-medium text-stone-700">
              Password
            </label>
            <Link to="/forgot-password" className="text-[13px] font-medium text-brand-700 hover:text-brand-800">
              Forgot password?
            </Link>
          </div>
          <div className="relative mt-1.5">
            <input
              id="password"
              name="password"
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
              value={values.password}
              onChange={handleChange}
              onKeyDown={handlePasswordKey}
              onKeyUp={handlePasswordKey}
              onBlur={() => setCapsLockOn(false)}
              aria-invalid={Boolean(errors.password)}
              aria-describedby={errors.password ? passwordErrorId : undefined}
              className={`${refinedInputClasses(Boolean(errors.password))} pr-20`}
            />
            {/* Caps Lock shows as a small ⇪ inside the field, like a desktop login screen; the words
                below the field say the same, so the symbol never has to be recognised on its own. */}
            {capsLockOn && (
              <span className="pointer-events-none absolute inset-y-0 right-11 flex items-center text-stone-500">
                <ArrowBigUpDash className="size-[18px]" strokeWidth={1.75} aria-hidden="true" />
              </span>
            )}
            <button
              type="button"
              onClick={() => setShowPassword((current) => !current)}
              aria-label={showPassword ? 'Hide password' : 'Show password'}
              aria-pressed={showPassword}
              className="absolute inset-y-0 right-0 flex items-center rounded-r-xl px-3.5 text-stone-500 hover:text-stone-700"
            >
              {showPassword ? (
                <EyeOff className="size-[18px]" strokeWidth={1.75} aria-hidden="true" />
              ) : (
                <Eye className="size-[18px]" strokeWidth={1.75} aria-hidden="true" />
              )}
            </button>
          </div>
          {/* Always rendered so screen readers announce the warning when it appears (WCAG 4.1.3). */}
          <div aria-live="polite">
            {capsLockOn && <p className="mt-1.5 text-[13px] font-medium text-amber-700">Caps Lock is on.</p>}
          </div>
          {errors.password && (
            <p id={passwordErrorId} className="mt-1.5 text-[13px] text-brand-700">
              {errors.password}
            </p>
          )}
        </div>

        <button
          type="submit"
          disabled={submitting}
          className="mt-2 inline-flex h-[50px] w-full items-center justify-center gap-2 rounded-xl bg-brand-600 px-4 text-base font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-80 motion-safe:animate-rise-soft"
          style={rise(0.46)}
        >
          {submitting && <LoaderCircle className="size-[18px] motion-safe:animate-spin" strokeWidth={2.25} aria-hidden="true" />}
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <p className="mt-6 text-center text-sm text-stone-500 motion-safe:animate-rise-soft" style={rise(0.52)}>
        New client?{' '}
        <Link to="/enquire" className="font-medium text-brand-700 hover:text-brand-800">
          Start an enquiry
        </Link>
      </p>
    </AuthLayout>
  )
}
