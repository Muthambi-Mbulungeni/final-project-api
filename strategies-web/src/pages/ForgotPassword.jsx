import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import AuthLayout from '../components/layout/AuthLayout'
import TextField from '../components/ui/TextField'
import { apiPost } from '../lib/api'
import { useDocumentTitle } from '../lib/useDocumentTitle'
import { emailError } from '../lib/validation'

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
      <AuthLayout>
        <h1 className="text-center text-xl font-semibold tracking-tight text-stone-900">Check your email</h1>
        <p className="mt-3 text-center text-sm leading-relaxed text-stone-600">
          If an account exists for <span className="font-medium text-stone-900">{sentTo}</span>, you'll receive a
          link to reset your password shortly.
        </p>
        <div className="mt-8 text-center">
          <BackToSignIn />
        </div>
      </AuthLayout>
    )
  }

  return (
    <AuthLayout>
      <h1 className="text-center text-xl font-semibold tracking-tight text-stone-900">Reset your password</h1>
      <p className="mt-1.5 text-center text-sm text-stone-600">
        Enter the email address for your account and we'll send you a reset link.
      </p>

      <form noValidate onSubmit={handleSubmit} className="mt-8 space-y-5">
        {formError && (
          <div role="alert" className="rounded-md border border-brand-200 bg-brand-50 px-3 py-2.5 text-sm text-brand-800">
            {formError}
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
        />

        <button
          type="submit"
          disabled={submitting}
          className="w-full rounded-md bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-70"
        >
          {submitting ? 'Sending…' : 'Send reset link'}
        </button>
      </form>

      <div className="mt-8 text-center">
        <BackToSignIn />
      </div>
    </AuthLayout>
  )
}
