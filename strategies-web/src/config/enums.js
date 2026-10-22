// Mirrors the backend enums in Strategies.Api/Data/Enums (origin/develop @ 9a85c57).
// Order matters: .NET sends enums as their index unless JsonStringEnumConverter is enabled,
// so resolveEnum accepts either the index (number) or the name (string).

export const ENUMS = {
  caseStage: [
    { value: 'InitialIntake', label: 'Initial intake', tone: 'neutral' },
    { value: 'DocumentGathering', label: 'Document gathering', tone: 'info' },
    { value: 'UnderReview', label: 'Under review', tone: 'warning' },
    { value: 'SubmittedToAuthority', label: 'Submitted to authority', tone: 'info' },
    { value: 'FinalDecision', label: 'Final decision', tone: 'success' },
  ],
  triageStatus: [
    { value: 'Pending', label: 'Pending', tone: 'neutral' },
    { value: 'Qualified', label: 'Qualified', tone: 'success' },
    { value: 'Unqualified', label: 'Unqualified', tone: 'danger' },
    { value: 'NeedsReview', label: 'Needs review', tone: 'warning' },
  ],
  documentStatus: [
    { value: 'Pending', label: 'Pending', tone: 'warning' },
    { value: 'Approved', label: 'Approved', tone: 'success' },
    { value: 'Rejected', label: 'Rejected', tone: 'danger' },
  ],
  immigrationStatus: [
    { value: 'Citizen', label: 'Citizen', tone: 'neutral' },
    { value: 'PermanentResident', label: 'Permanent resident', tone: 'neutral' },
    { value: 'TemporaryResident', label: 'Temporary resident', tone: 'neutral' },
    { value: 'Foreigner', label: 'Foreign national', tone: 'neutral' },
  ],
  userRole: [
    { value: 'Admin', label: 'Administrator', tone: 'neutral' },
    { value: 'Consultant', label: 'Consultant', tone: 'neutral' },
    { value: 'Client', label: 'Client', tone: 'neutral' },
  ],
  runStatus: [
    { value: 'Success', label: 'Success', tone: 'success' },
    { value: 'Fallback', label: 'Fallback', tone: 'warning' },
    { value: 'Error', label: 'Error', tone: 'danger' },
  ],
  agentType: [
    { value: 'Triage', label: 'Triage', tone: 'neutral' },
    { value: 'Checklist', label: 'Checklist', tone: 'neutral' },
    { value: 'Summarisation', label: 'Summarisation', tone: 'neutral' },
  ],
}

export function resolveEnum(type, value) {
  const options = ENUMS[type]
  if (!options || value === null || value === undefined) return null
  if (typeof value === 'number') return options[value] ?? null
  return options.find((option) => option.value === value) ?? null
}

export function enumOrder(type, value) {
  const option = resolveEnum(type, value)
  return option ? ENUMS[type].indexOf(option) : null
}

export function enumFilter(type, key, label) {
  return {
    key,
    label,
    options: ENUMS[type].map((option) => ({ value: option.value, label: option.label })),
    getValue: (row) => resolveEnum(type, row[key])?.value ?? row[key],
  }
}
