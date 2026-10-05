import { inputClasses, refinedInputClasses } from './fieldStyles'

// Labelled text input with an optional error message and an optional
// element beside the label (e.g. a "Forgot password?" link).
// `refined` gives the larger, softer field used on the sign-in pages.
export default function TextField({ id, label, error, labelAction, refined = false, ...inputProps }) {
  const errorId = `${id}-error`
  const classes = refined ? refinedInputClasses(Boolean(error)) : inputClasses(Boolean(error))

  return (
    <div>
      <div className="flex items-center justify-between gap-4">
        <label
          htmlFor={id}
          className={`block font-medium ${refined ? 'text-[13px] text-stone-700' : 'text-sm text-stone-800'}`}
        >
          {label}
        </label>
        {labelAction}
      </div>
      <input
        id={id}
        name={id}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        className={`mt-1.5 ${classes}`}
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
