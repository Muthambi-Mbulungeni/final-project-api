import { inputClasses } from './fieldStyles'

// Labelled text input with an optional error message and an optional
// element beside the label (e.g. a "Forgot password?" link).
export default function TextField({ id, label, error, labelAction, ...inputProps }) {
  const errorId = `${id}-error`

  return (
    <div>
      <div className="flex items-center justify-between gap-4">
        <label htmlFor={id} className="block text-sm font-medium text-stone-800">
          {label}
        </label>
        {labelAction}
      </div>
      <input
        id={id}
        name={id}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : undefined}
        className={`mt-1.5 ${inputClasses(Boolean(error))}`}
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
