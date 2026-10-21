import { useState } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { ArrowLeft, CircleAlert, Eye, EyeOff, LoaderCircle } from 'lucide-react'
import AuthLayout from '../components/layout/AuthLayout'
import { refinedInputClasses } from '../components/ui/fieldStyles'
import { apiPost } from '../lib/api'
import { RESET_PASSWORD_PATH, resetErrorOutcome, resetPasswordBody, resetTokenFrom, validateResetPassword } from '../lib/resetPassword'
import { useDocumentTitle } from '../lib/useDocumentTitle'

// Field ids in on-screen order, used to move focus to the first invalid field.
const FIELD_ORDER = ['password', 'confirm']

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

// No maxLength and no paste blocking: a cut-off pasted password would be saved without the person knowing.
function PasswordField({ id, label, hint, value, onChange, error }) {
  const [visible, setVisible] = useState(false)
  const hintId = `${id}-hint`
  const errorId = `${id}-error`
  const describedBy = [hint && hintId, error && errorId].filter(Boolean).join(' ')

  return (
    <div>
      <label htmlFor={id} className="block text-[13px] font-medium text-stone-700">
        {label}
      </label>
      {hint && (
        <p id={hintId} className="mt-1 text-[13px] text-stone-500">
          {hint}
        </p>
      )}
      <div className="relative mt-1.5">
        <input
          id={id}
          name={id}
          type={visible ? 'text' : 'password'}
          autoComplete="new-password"
          value={value}
          onChange={onChange}
          aria-invalid={Boolean(error)}
          aria-describedby={describedBy || undefined}
          className={`${refinedInputClasses(Boolean(error))} pr-12`}
        />
        <button
          type="button"
          onClick={() => setVisible((current) => !current)}
          aria-label={visible ? `Hide ${label.toLowerCase()}` : `Show ${label.toLowerCase()}`}
          aria-pressed={visible}
          className="absolute inset-y-0 right-0 flex items-center rounded-r-xl px-3.5 text-stone-500 hover:text-stone-700"
        >
          {visible ? (
            <EyeOff className="size-[18px]" strokeWidth={1.75} aria-hidden="true" />
          ) : (
            <Eye className="size-[18px]" strokeWidth={1.75} aria-hidden="true" />
          )}
        </button>
      </div>
      {error && (
        <p id={errorId} className="mt-1.5 text-[13px] text-brand-700">
          {error}
        </p>
      )}
    </div>
  )
}

// Shown instead of the form when the link has no token, or the API says it cannot be used.
function LinkProblem({ message }) {
  return (
    <AuthLayout journey>
      <div className="text-center motion-safe:animate-rise-soft">
        <span className="mx-auto flex size-12 items-center justify-center rounded-full bg-brand-50 text-brand-700">
          <CircleAlert className="size-6" strokeWidth={1.75} aria-hidden="true" />
        </span>
        <h1 className="mt-5 text-[26px] font-semibold tracking-[-0.022em] text-stone-900 lg:text-[30px]">
          This link cannot be used
        </h1>
        <p role="alert" className="mt-3 text-[15px] leading-relaxed text-stone-500">
          {message} Reset links expire after a short time, so request a new one.
        </p>
        <Link
          to="/forgot-password"
          className="mt-7 inline-flex h-[50px] w-full items-center justify-center rounded-xl bg-brand-600 px-4 text-base font-semibold text-white transition-colors hover:bg-brand-700"
        >
          Request a new reset link
        </Link>
        <div className="mt-6">
          <BackToSignIn />
        </div>
      </div>
    </AuthLayout>
  )
}

export default function ResetPassword() {
  useDocumentTitle('Choose a new password')
  const location = useLocation()
  const navigate = useNavigate()
  const token = resetTokenFrom(location.search)
  const [values, setValues] = useState({ password: '', confirm: '' })
  const [errors, setErrors] = useState({})
  const [formError, setFormError] = useState('')
  const [linkError, setLinkError] = useState('')
  const [submitting, setSubmitting] = useState(false)

  function handleChange(event) {
    const { name, value } = event.target
    setValues((previous) => ({ ...previous, [name]: value }))
    setFormError('')
    if (errors[name]) {
      setErrors((previous) => ({ ...previous, [name]: undefined }))
    }
  }

  async function handleSubmit(event) {
    event.preventDefault()
    const found = validateResetPassword(values)
    setErrors(found)

    const firstInvalid = FIELD_ORDER.find((field) => found[field])
    if (firstInvalid) {
      document.getElementById(firstInvalid)?.focus()
      return
    }

    setSubmitting(true)
    setFormError('')

    try {
      await apiPost(RESET_PASSWORD_PATH, resetPasswordBody(token, values.password))
      navigate('/login', { replace: true, state: { reason: 'password-reset' } })
    } catch (error) {
      const outcome = resetErrorOutcome(error)
      if (outcome.kind === 'token') {
        setLinkError(outcome.message)
      } else if (outcome.kind === 'field') {
        setErrors({ password: outcome.message })
        document.getElementById('password')?.focus()
      } else {
        setFormError(outcome.message)
      }
      setSubmitting(false)
    }
  }

  if (!token) return <LinkProblem message="This reset link is incomplete." />
  if (linkError) return <LinkProblem message={linkError} />

  return (
    <AuthLayout journey>
      <h1
        className="text-center text-[26px] font-semibold tracking-[-0.022em] text-stone-900 motion-safe:animate-rise-soft lg:text-[30px]"
        style={rise(0.22)}
      >
        Choose a new password
      </h1>
      <p className="mt-1.5 text-center text-[15px] text-stone-500 motion-safe:animate-rise-soft" style={rise(0.27)}>
        Enter your new password twice. You'll sign in with it next.
      </p>

      <form noValidate onSubmit={handleSubmit} className="mt-7 space-y-5">
        {formError && (
          <div role="alert" className="rounded-xl bg-brand-50 px-3.5 py-2.5 text-sm text-brand-800">
            {formError}
          </div>
        )}

        <div className="motion-safe:animate-rise-soft" style={rise(0.34)}>
          <PasswordField
            id="password"
            label="New password"
            hint="12 to 64 characters. A short phrase you'll remember works well, and spaces are fine."
            value={values.password}
            onChange={handleChange}
            error={errors.password}
          />
        </div>

        <div className="motion-safe:animate-rise-soft" style={rise(0.4)}>
          <PasswordField
            id="confirm"
            label="Confirm new password"
            value={values.confirm}
            onChange={handleChange}
            error={errors.confirm}
          />
        </div>

        <button
          type="submit"
          disabled={submitting}
          className="mt-2 inline-flex h-[50px] w-full items-center justify-center gap-2 rounded-xl bg-brand-600 px-4 text-base font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-80 motion-safe:animate-rise-soft"
          style={rise(0.46)}
        >
          {submitting && <LoaderCircle className="size-[18px] motion-safe:animate-spin" strokeWidth={2.25} aria-hidden="true" />}
          {submitting ? 'Saving…' : 'Reset password'}
        </button>
      </form>

      <div className="mt-6 text-center motion-safe:animate-rise-soft" style={rise(0.52)}>
        <BackToSignIn />
      </div>
    </AuthLayout>
  )
}
