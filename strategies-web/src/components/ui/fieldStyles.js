// Classes for text inputs, with an error variant.
// Focus uses the global :focus-visible outline (index.css), so inputs, links and
// buttons share one visible focus style (WCAG 2.4.7, 1.4.11). Do not add outline-none here.
// Larger, softer fields for the sign-in pages. A deliberate exception to the outline rule above:
// on focus the whole border turns brand red (5.6:1 against white, so it still meets WCAG 1.4.11
// and 2.4.7) with a faint glow around it, which reads calmer on a page that is mostly form.
export function refinedInputClasses(hasError) {
  return `block h-[50px] w-full rounded-xl border bg-white px-3.5 text-base text-stone-900 placeholder:text-stone-500 transition-[border-color,box-shadow] duration-200 focus-visible:border-brand-600 focus-visible:outline-none focus-visible:ring-4 focus-visible:ring-brand-600/15 ${
    hasError ? 'border-brand-600' : 'border-stone-300'
  }`
}

export function inputClasses(hasError) {
  return `block w-full rounded-md border bg-white px-3 py-2.5 text-sm text-stone-900 placeholder:text-stone-500 ${
    hasError ? 'border-brand-600' : 'border-stone-300 focus:border-stone-500'
  }`
}
