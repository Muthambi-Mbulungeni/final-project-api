import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useLocation } from 'react-router-dom'
import { ArrowLeft, CircleAlert, CircleCheck, LoaderCircle } from 'lucide-react'
import AuthLayout from '../components/layout/AuthLayout'
import TextField from '../components/ui/TextField'
import { apiPost } from '../lib/api'
import { CONFIRM_EMAIL_PATH, confirmErrorOutcome, confirmationTokenFrom } from '../lib/confirmEmail'
import { useDocumentTitle } from '../lib/useDocumentTitle'
import { useResendConfirmation } from '../lib/useResendConfirmation'
import { emailError } from '../lib/validation'

const primaryButton =
  'inline-flex h-[50px] w-full items-center justify-center gap-2 rounded-xl bg-brand-600 px-4 text-base font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-80'

function BackToSignIn() {
  return (
    <Link
      to="/login"
      className="inline-flex items-center gap-1.5 text-sm font-medium text-stone-600 hover:text-stone-900"
    >
      <ArrowLeft className="size-4" strokeWidth={1.75} aria-hidden="true" />
      Back to sign in
    </Link>
  )
}

// The result heading takes focus when it appears, so keyboard and screen-reader users start there.
function ResultHeading({ children }) {
  const ref = useRef(null)
  useEffect(() => {
    ref.current?.focus()
  }, [])
  return (
    <h1
      ref={ref}
      tabIndex={-1}
      className="mt-5 text-[26px] font-semibold tracking-[-0.022em] text-stone-900 outline-none lg:text-[30px]"
    >
      {children}
    </h1>
  )
}

function ResendForm() {
  const [email, setEmail] = useState('')
  const [fieldError, setFieldError] = useState('')
  const resend = useResendConfirmation()

  function handleChange(event) {
    setEmail(event.target.value)
    setFieldError('')
    resend.reset()
  }

  function handleSubmit(event) {
    event.preventDefault()
    const message = emailError(email)
    if (message) {
      setFieldError(message)
      document.getElementById('email')?.focus()
      return
    }
    resend.send(email)
  }

  return (
    <form noValidate onSubmit={handleSubmit} className="mt-7 space-y-5 text-left">
      {resend.error && (
        <div role="alert" className="rounded-xl bg-brand-50 px-3.5 py-2.5 text-sm text-brand-800">
          {resend.error}
        </div>
      )}
      <TextField
        id="email"
        label="Email address"
        type="email"
        autoComplete="email"
        value={email}
        onChange={handleChange}
        error={fieldError}
        refined
      />
      <button type="submit" disabled={resend.sending} className={primaryButton}>
        {resend.sending && <LoaderCircle className="size-[18px] motion-safe:animate-spin" strokeWidth={2.25} aria-hidden="true" />}
        {resend.sending ? 'Sending…' : 'Send a new link'}
      </button>
      {/* Always rendered so the sent message is announced when it appears. */}
      <div role="status">
        {resend.message && (
          <p className="flex items-start gap-2 rounded-xl bg-stone-100 px-3.5 py-2.5 text-sm text-stone-700">
            <CircleCheck className="mt-px size-4 shrink-0 text-emerald-600" strokeWidth={2} aria-hidden="true" />
            {resend.message}
          </p>
        )}
      </div>
    </form>
  )
}

export default function ConfirmEmail() {
  useDocumentTitle('Confirm your email')
  const location = useLocation()
  const token = confirmationTokenFrom(location.search)
  // checking | confirmed | invalid | error. No token means the link is incomplete: nothing to send.
  const [status, setStatus] = useState(token ? 'checking' : 'invalid')
  const [errorMessage, setErrorMessage] = useState('')
  // The token already sent. StrictMode runs effects twice in development, and a token works only
  // once, so a second call would turn a success into "invalid".
  const sentToken = useRef(null)

  const confirm = useCallback(async () => {
    setStatus('checking')
    try {
      await apiPost(CONFIRM_EMAIL_PATH, { token })
      setStatus('confirmed')
    } catch (error) {
      const outcome = confirmErrorOutcome(error)
      setErrorMessage(outcome.message)
      setStatus(outcome.kind)
    }
  }, [token])

  useEffect(() => {
    if (!token || sentToken.current === token) return
    sentToken.current = token
    confirm()
  }, [token, confirm])

  if (status === 'checking') {
    return (
      <AuthLayout journey>
        <div role="status" className="flex flex-col items-center py-10 text-center">
          <LoaderCircle className="size-8 text-brand-600 motion-safe:animate-spin" strokeWidth={2} aria-hidden="true" />
          <p className="mt-4 text-[15px] text-stone-500">Confirming your email…</p>
        </div>
      </AuthLayout>
    )
  }

  if (status === 'confirmed') {
    return (
      <AuthLayout journey>
        <div className="text-center motion-safe:animate-rise-soft">
          <span className="mx-auto flex size-12 items-center justify-center rounded-full bg-emerald-50 text-emerald-700">
            <CircleCheck className="size-6" strokeWidth={1.75} aria-hidden="true" />
          </span>
          <ResultHeading>Email confirmed</ResultHeading>
          <p role="status" className="mt-3 text-[15px] leading-relaxed text-stone-500">
            Your account is ready. Sign in with the email address and password you registered with.
          </p>
          <Link to="/login" state={{ reason: 'email-confirmed' }} className={`mt-7 ${primaryButton}`}>
            Sign in
          </Link>
        </div>
      </AuthLayout>
    )
  }

  if (status === 'error') {
    return (
      <AuthLayout journey>
        <div className="text-center motion-safe:animate-rise-soft">
          <span className="mx-auto flex size-12 items-center justify-center rounded-full bg-brand-50 text-brand-700">
            <CircleAlert className="size-6" strokeWidth={1.75} aria-hidden="true" />
          </span>
          <ResultHeading>We couldn't confirm your email</ResultHeading>
          <p role="alert" className="mt-3 text-[15px] leading-relaxed text-stone-500">
            {errorMessage}
          </p>
          <button type="button" onClick={confirm} className={`mt-7 ${primaryButton}`}>
            Try again
          </button>
          <div className="mt-6">
            <BackToSignIn />
          </div>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout journey>
      <div className="text-center motion-safe:animate-rise-soft">
        <span className="mx-auto flex size-12 items-center justify-center rounded-full bg-brand-50 text-brand-700">
          <CircleAlert className="size-6" strokeWidth={1.75} aria-hidden="true" />
        </span>
        <ResultHeading>This link is invalid or has expired</ResultHeading>
        <p role="alert" className="mt-3 text-[15px] leading-relaxed text-stone-500">
          Confirmation links work once and expire after 24 hours. Enter your email address and we'll send you a new
          one. Already confirmed?{' '}
          <Link to="/login" className="font-medium text-brand-700 hover:text-brand-800">
            Sign in
          </Link>
          .
        </p>
      </div>
      <ResendForm />
      <div className="mt-6 text-center">
        <BackToSignIn />
      </div>
    </AuthLayout>
  )
}
