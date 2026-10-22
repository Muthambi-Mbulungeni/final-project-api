// Where the toast region is rendered. An open modal <dialog> sits in the browser's top layer and
// makes the rest of the page inert, so a toast rendered at the app root would be hidden behind the
// panel and never announced. While a dialog is open, the region moves inside the last one instead.
// Every dialog in the portal is opened with showModal(), so an open dialog is a modal one.
export function toastHost(doc) {
  const open = doc.querySelectorAll('dialog[open]')
  return open.length > 0 ? open[open.length - 1] : doc.body
}
