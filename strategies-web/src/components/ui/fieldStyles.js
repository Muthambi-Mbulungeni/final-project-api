// Classes for text inputs, with an error variant.
// Focus uses the global :focus-visible outline (index.css), so inputs, links and
// buttons share one visible focus style (WCAG 2.4.7, 1.4.11). Do not add outline-none here.
export function inputClasses(hasError) {
  return `block w-full rounded-md border bg-white px-3 py-2.5 text-sm text-stone-900 placeholder:text-stone-500 ${
    hasError ? 'border-brand-600' : 'border-stone-300 focus:border-stone-500'
  }`
}
