import AuditLogPage from '../pages/admin/AuditLogPage'
import UsersPage from '../pages/admin/UsersPage'
import { AdminCasesPage, ConsultantCasesPage } from '../pages/cases/CasesPage'
import ClientDocumentsPage from '../pages/client/ClientDocumentsPage'
import LeadTriage from '../pages/consultant/LeadTriage'

// Pages that have been built, keyed by navigation path.
// The router and the sidebar both use this, so unbuilt pages never appear as links.
export const MODULE_PAGES = {
  '/admin/users': UsersPage,
  '/admin/cases': AdminCasesPage,
  '/admin/audit-log': AuditLogPage,
  '/consultant/leads': LeadTriage,
  '/consultant/cases': ConsultantCasesPage,
  '/client/documents': ClientDocumentsPage,
}

// Overview items (end: true) are always built; other items only when they have a page.
export function isBuilt(item) {
  return Boolean(item.end || MODULE_PAGES[item.to])
}
