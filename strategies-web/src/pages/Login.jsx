import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import logo from '../assets/strategies-logo.png'
import { ROLES, startDevSession } from '../lib/session'

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

function validate(values) {
  const errors = {}
  const email = values.email.trim()

  if (!email) {
    errors.email = 'Enter your email address.'
  } else if (!EMAIL_PATTERN.test(email)) {
    errors.email = 'Enter a valid email address, like name@example.com.'
  }

  if (!values.password) {
    errors.password = 'Enter your password.'
  }

  return errors
}

function Field({ id, label, error, ...inputProps }) {
  const errorId = `${id}-error`

  return (
    <div>
      <label htmlFor={id} className="block text-sm font-medium text-stone-800">
        {label}
      </label>
      <input
        id={id}
        name={id}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        className={`mt-1.5 block w-full rounded-md border bg-white px-3 py-2.5 text-sm text-stone-900 placeholder:text-stone-400 focus:outline-none focus:ring-2 ${
          error
            ? 'border-brand-600 focus:ring-brand-600/20'
            : 'border-stone-300 focus:border-stone-500 focus:ring-stone-900/10'
        }`}
        {...inputProps}
      />
      {error && (
        <p id={errorId} className="mt-1.5 text-sm text-brand-700">
          {error}
        </p>
      )}
    </div>
  )
}

export default function Login() {
  const navigate = useNavigate()
  const [values, setValues] = useState({ email: '', password: '' })
  const [errors, setErrors] = useState({})
  const [notice, setNotice] = useState('')

  function handleChange(event) {
    const { name, value } = event.target
    setValues((previous) => ({ ...previous, [name]: value }))
    if (errors[name]) {
      setErrors((previous) => ({ ...previous, [name]: undefined }))
    }
  }

  function handleSubmit(event) {
    event.preventDefault()
    const found = validate(values)
    setErrors(found)
    if (Object.keys(found).length > 0) return

    setNotice(
      'Email and password sign-in becomes available once authentication is connected. Use a demo workspace below for now.',
    )
  }

  function enterAs(role) {
    startDevSession(role)
    navigate(ROLES[role].home, { replace: true })
  }

  return (
    <div className="flex min-h-screen flex-col bg-white">
      <header className="px-6 py-6 sm:px-10">
        <img src={logo} alt="Strategies Migration Services" className="h-11 w-auto" />
      </header>

      <main className="flex flex-1 items-start justify-center px-6 pt-6 pb-16 sm:pt-16">
        <div className="w-full max-w-sm">
          <h1 className="text-2xl font-semibold tracking-tight text-stone-900">Sign in to your portal</h1>
          <p className="mt-1.5 text-sm text-stone-600">
            Track your application, upload documents and message your consultant.
          </p>

          <form noValidate onSubmit={handleSubmit} className="mt-8 space-y-5">
            <Field
              id="email"
              label="Email address"
              type="email"
              autoComplete="email"
              value={values.email}
              onChange={handleChange}
              error={errors.email}
            />
            <Field
              id="password"
              label="Password"
              type="password"
              autoComplete="current-password"
              value={values.password}
              onChange={handleChange}
              error={errors.password}
            />

            {notice && (
              <p role="status" className="rounded-md border border-stone-200 bg-white px-3 py-2.5 text-sm text-stone-700">
                {notice}
              </p>
            )}

            <button
              type="submit"
              className="w-full rounded-md bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white transition-colors hover:bg-brand-700"
            >
              Sign in
            </button>
          </form>

          <div className="mt-10 border-t border-stone-200 pt-6">
            <p className="text-xs font-semibold uppercase tracking-[0.08em] text-stone-500">Demo workspaces</p>
            <p className="mt-1 text-sm text-stone-600">Development only. Removed once authentication is connected.</p>
            <div className="mt-4 grid grid-cols-3 gap-2">
              {Object.values(ROLES).map((role) => (
                <button
                  key={role.key}
                  type="button"
                  onClick={() => enterAs(role.key)}
                  className="rounded-md border border-stone-300 bg-white px-2 py-2 text-sm font-medium text-stone-800 transition-colors hover:border-stone-400 hover:bg-stone-50"
                >
                  {role.label}
                </button>
              ))}
            </div>
          </div>
        </div>
      </main>

      <footer className="px-6 py-6 text-xs text-stone-500 sm:px-10">
        Strategies Migration Services. Personal information is handled in accordance with POPIA.
      </footer>
    </div>
  )
}
