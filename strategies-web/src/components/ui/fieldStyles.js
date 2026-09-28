// Classes for text inputs, with an error variant.
export function inputClasses(hasError) {
  return `block w-full rounded-md border bg-white px-3 py-2.5 text-sm text-stone-900 placeholder:text-stone-400 focus:outline-none focus:ring-2 ${
    hasError
      ? 'border-brand-600 focus:ring-brand-600/20'
      : 'border-stone-300 focus:border-stone-500 focus:ring-stone-900/10'
  }`
}
