import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft, LoaderCircle, MailCheck } from 'lucide-react'
import AuthLayout from '../components/layout/AuthLayout'
import TextField from '../components/ui/TextField'
import { apiPost } from '../lib/api'
import { useDocumentTitle } from '../lib/useDocumentTitle'
import { emailError } from '../lib/validation'

// Staggered entrance; motion-safe only.
const rise = (seconds) => ({ animationDelay: `${seconds}s` })

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

export default function ForgotPassword() {
  useDocumentTitle('Reset password')
  const [email, setEmail] = useState('')
  const [fieldError, setFieldError] = useState('')
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [sentTo, setSentTo] = useState('')

  function handleChange(event) {
    setEmail(event.target.value)
    setFieldError('')
    setFormError('')
  }

  async function handleSubmit(event) {
    event.preventDefault()
    const message = emailError(email)
    if (message) {
      setFieldError(message)
      return
    }

    setSubmitting(true)
    setFormError('')

    try {
      await apiPost('/api/auth/forgot-password', { email: email.trim() })
      setSentTo(email.trim())
    } catch (error) {
      setFormError(
        error.status === 429 ? 'Too many requests. Wait a minute and try again.' : error.message,
      )
    } finally {
      setSubmitting(false)
    }
  }

  // The same confirmation is shown whether or not the account exists,
  // so the form cannot be used to discover which emails are registered.
  if (sentTo) {
    return (
      <AuthLayout journey>
        <div className="text-center motion-safe:animate-rise-soft">
          <span className="mx-auto flex size-12 items-center justify-center rounded-full bg-stone-100 text-stone-700">
            <MailCheck className="size-6" strokeWidth={1.75} aria-hidden="true" />
          </span>
          <h1 className="mt-5 text-[26px] font-semibold tracking-[-0.022em] text-stone-900 lg:text-[30px]">
            Check your email
          </h1>
          <p className="mt-3 text-[15px] leading-relaxed text-stone-500">
            If an account exists for <span className="font-medium text-stone-900">{sentTo}</span>, you'll receive a
            link to reset your password shortly.
          </p>
          <div className="mt-8">
            <BackToSignIn />
          </div>
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout journey>
      <h1
        className="text-center text-[26px] font-semibold tracking-[-0.022em] text-stone-900 motion-safe:animate-rise-soft lg:text-[30px]"
        style={rise(0.22)}
      >
        Reset your password
      </h1>
      <p className="mt-1.5 text-center text-[15px] text-stone-500 motion-safe:animate-rise-soft" style={rise(0.27)}>
        Enter the email address for your account and we'll send you a reset link.
      </p>

      <form noValidate onSubmit={handleSubmit} className="mt-7 space-y-5">
        {formError && (
          <div role="alert" className="rounded-xl bg-brand-50 px-3.5 py-2.5 text-sm text-brand-800">
            {formError}
          </div>
        )}

        <div className="motion-safe:animate-rise-soft" style={rise(0.34)}>
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
        </div>

        <button
          type="submit"
          disabled={submitting}
          className="mt-2 inline-flex h-[50px] w-full items-center justify-center gap-2 rounded-xl bg-brand-600 px-4 text-base font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-80 motion-safe:animate-rise-soft"
          style={rise(0.4)}
        >
          {submitting && <LoaderCircle className="size-[18px] motion-safe:animate-spin" strokeWidth={2.25} aria-hidden="true" />}
          {submitting ? 'Sending…' : 'Send reset link'}
        </button>
      </form>

      <div className="mt-6 text-center motion-safe:animate-rise-soft" style={rise(0.46)}>
        <BackToSignIn />
      </div>
    </AuthLayout>
  )
}
