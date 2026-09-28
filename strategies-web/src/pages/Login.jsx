import { useState } from 'react'
import { Link } from 'react-router-dom'
import { Eye, EyeOff } from 'lucide-react'
import AuthLayout from '../components/layout/AuthLayout'
import { inputClasses } from '../components/ui/fieldStyles'
import TextField from '../components/ui/TextField'
import { apiPost } from '../lib/api'
import { signInErrorMessage } from '../lib/authMessages'
import { useDocumentTitle } from '../lib/useDocumentTitle'
import { validateSignIn } from '../lib/validation'

export default function Login() {
  useDocumentTitle('Sign in')
  const [values, setValues] = useState({ email: '', password: '' })
  const [errors, setErrors] = useState({})
  const [formError, setFormError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [showPassword, setShowPassword] = useState(false)
  const [capsLockOn, setCapsLockOn] = useState(false)

  function handleChange(event) {
    const { name, value } = event.target
    setValues((previous) => ({ ...previous, [name]: value }))
    setFormError('')
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
    if (Object.keys(found).length > 0) return

    setSubmitting(true)
    setFormError('')

    try {
      await apiPost('/api/auth/login', { email: values.email.trim(), password: values.password })
      // The login response contract (token, expiry, role) has not been published yet,
      // so a successful response cannot start a session. Finish this once it is.
      setFormError(signInErrorMessage({ status: 501 }))
    } catch (error) {
      setFormError(signInErrorMessage(error))
    } finally {
      setSubmitting(false)
    }
  }

  const passwordErrorId = 'password-error'

  return (
    <AuthLayout>
      <h1 className="text-center text-xl font-semibold tracking-tight text-stone-900">Sign in</h1>
      <p className="mt-1.5 text-center text-sm text-stone-600">Strategies Migration Services portal</p>

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
          value={values.email}
          onChange={handleChange}
          error={errors.email}
        />

        <div>
          <div className="flex items-center justify-between gap-4">
            <label htmlFor="password" className="block text-sm font-medium text-stone-800">
              Password
            </label>
            <Link to="/forgot-password" className="text-sm font-medium text-brand-700 hover:text-brand-800">
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
              aria-invalid={Boolean(errors.password)}
              aria-describedby={errors.password ? passwordErrorId : undefined}
              className={`${inputClasses(Boolean(errors.password))} pr-11`}
            />
            <button
              type="button"
              onClick={() => setShowPassword((current) => !current)}
              aria-label={showPassword ? 'Hide password' : 'Show password'}
              aria-pressed={showPassword}
              className="absolute inset-y-0 right-0 flex items-center px-3 text-stone-400 hover:text-stone-700"
            >
              {showPassword ? (
                <EyeOff className="size-4" strokeWidth={1.75} aria-hidden="true" />
              ) : (
                <Eye className="size-4" strokeWidth={1.75} aria-hidden="true" />
              )}
            </button>
          </div>
          {capsLockOn && <p className="mt-1.5 text-xs font-medium text-amber-700">Caps Lock is on.</p>}
          {errors.password && (
            <p id={passwordErrorId} className="mt-1.5 text-sm text-brand-700">
              {errors.password}
            </p>
          )}
        </div>

        <button
          type="submit"
          disabled={submitting}
          className="w-full rounded-md bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700 disabled:cursor-wait disabled:opacity-70"
        >
          {submitting ? 'Signing in…' : 'Sign in'}
        </button>
      </form>

      <p className="mt-8 border-t border-stone-200 pt-6 text-center text-sm text-stone-600">
        New client?{' '}
        <Link to="/enquire" className="font-medium text-brand-700 hover:text-brand-800">
          Start an enquiry
        </Link>
      </p>
    </AuthLayout>
  )
}
