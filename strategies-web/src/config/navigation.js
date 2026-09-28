import {
  Briefcase,
  Download,
  FolderOpen,
  History,
  Inbox,
  LayoutDashboard,
  MessageSquare,
  UserRound,
  Users,
} from 'lucide-react'

export const WORKSPACE_LABELS = {
  admin: 'Administration',
  consultant: 'Consultant workspace',
  client: 'Client portal',
}

export const NAVIGATION = {
  admin: [
    { to: '/admin', label: 'Overview', icon: LayoutDashboard, end: true },
    { to: '/admin/users', label: 'Users', icon: Users, description: 'View, edit, suspend and remove user accounts.' },
    { to: '/admin/leads', label: 'Leads', icon: Inbox, description: 'All enquiries captured by the intake agent.' },
    { to: '/admin/cases', label: 'Cases', icon: Briefcase, description: 'Every case across all consultants and stages.' },
    { to: '/admin/audit-log', label: 'Audit log', icon: History, description: 'Who changed what, and when, across core records.' },
    { to: '/admin/reports', label: 'Reports', icon: Download, description: 'Generate and export case and lead reports.' },
  ],
  consultant: [
    { to: '/consultant', label: 'Overview', icon: LayoutDashboard, end: true },
    { to: '/consultant/leads', label: 'Lead triage', icon: Inbox, description: 'Qualify new enquiries and convert them into cases.' },
    { to: '/consultant/cases', label: 'My cases', icon: Briefcase, description: 'Cases assigned to you, grouped by stage.' },
    { to: '/consultant/documents', label: 'Documents', icon: FolderOpen, description: 'Client uploads waiting for your review.' },
    { to: '/consultant/messages', label: 'Messages', icon: MessageSquare, description: 'Conversations with your clients.' },
  ],
  client: [
    { to: '/client', label: 'My case', icon: LayoutDashboard, end: true },
    { to: '/client/documents', label: 'Documents', icon: FolderOpen, description: 'Upload and track the documents your application needs.' },
    { to: '/client/messages', label: 'Messages', icon: MessageSquare, description: 'Updates and questions from your consultant.' },
    { to: '/client/profile', label: 'Profile', icon: UserRound, description: 'Your personal and contact details.' },
  ],
}

// Returns the navigation item for the current path, or null if none matches.
// Items marked `end` only match their exact path; others also match sub-paths
// (e.g. /admin/users/123 matches Users). The most specific match wins.
export function findCurrentItem(items, pathname) {
  const path = pathname.length > 1 ? pathname.replace(/\/+$/, '') : pathname
  const matches = items.filter(({ to, end }) => path === to || (!end && path.startsWith(`${to}/`)))
  return matches.sort((a, b) => b.to.length - a.to.length)[0] ?? null
}
