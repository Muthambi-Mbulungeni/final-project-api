// Mirrors the backend CaseStage enum and the five-stage case lifecycle.
export const CASE_STAGES = [
  { key: 'InitialIntake', label: 'Initial intake', clientHint: 'We assess your enquiry and eligibility.' },
  { key: 'DocumentGathering', label: 'Document gathering', clientHint: 'Upload the documents on your checklist.' },
  { key: 'UnderReview', label: 'Under review', clientHint: 'Your consultant checks your complete file.' },
  { key: 'SubmittedToAuthority', label: 'Submitted to authority', clientHint: 'Your application has been lodged.' },
  { key: 'FinalDecision', label: 'Final decision', clientHint: 'The outcome is received and shared with you.' },
]
