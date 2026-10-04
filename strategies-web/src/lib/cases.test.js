import { describe, expect, it } from 'vitest'
import {
  assignBody,
  assignErrorMessage,
  assignPath,
  assignedConsultantName,
  canChangeStage,
  consultantChoices,
  currentConsultantId,
  latestCase,
  stageActionLabel,
  stageBody,
  stageChangeError,
  stageErrorMessage,
  stagePath,
} from './cases'

describe('assigning a consultant', () => {
  const page = (data, totalRecords = data.length) => ({ data, totalRecords, totalPages: 1, currentPage: 1 })
  const users = [
    { id: 'c2', fullName: 'Sipho Dlamini', role: 'Consultant', isActive: false },
    { id: 'c1', fullName: 'Consultant User', role: 'Consultant', isActive: true },
    { id: 'a1', fullName: 'System Admin', role: 'Admin', isActive: true },
    { id: 'k1', fullName: 'John Doe', role: 'Client', isActive: true },
    { id: 'c3', fullName: 'Ayanda Khumalo', role: 1, isActive: true },
  ]

  it('sends the consultant id to the case assign endpoint', () => {
    expect(assignPath('a b')).toBe('/api/cases/a%20b/assign')
    expect(assignBody('c1')).toEqual({ consultantId: 'c1' })
  })

  it('offers only active consultants, by name, accepting the role as text or number', () => {
    const { consultants, complete } = consultantChoices(page(users))
    expect(consultants.map((consultant) => consultant.id)).toEqual(['c3', 'c1'])
    expect(complete).toBe(true)
  })

  it('says when the API holds more accounts than were loaded', () => {
    expect(consultantChoices(page(users, 140)).complete).toBe(false)
    expect(consultantChoices(null)).toEqual({ consultants: [], complete: true })
  })

  it('marks the current consultant only when their name is unique', () => {
    const consultants = [
      { id: 'c1', fullName: 'Consultant User' },
      { id: 'c4', fullName: 'Sam Smith' },
      { id: 'c5', fullName: 'Sam Smith' },
    ]
    expect(currentConsultantId(consultants, 'Consultant User')).toBe('c1')
    expect(currentConsultantId(consultants, 'Sam Smith')).toBeNull()
    expect(currentConsultantId(consultants, null)).toBeNull()
  })

  it('treats the API placeholder as no consultant', () => {
    expect(assignedConsultantName({ consultantName: 'Unassigned' })).toBeNull()
    expect(assignedConsultantName({ consultantName: ' Riaad ' })).toBe('Riaad')
    expect(assignedConsultantName({})).toBeNull()
  })

  it('explains failed assignments in plain language', () => {
    expect(assignErrorMessage({ status: 403 })).toMatch(/Only administrators/)
    expect(assignErrorMessage({ status: 404 })).toMatch(/no longer exists/)
    expect(assignErrorMessage({ status: 400, message: 'One or more validation errors occurred.' })).toMatch(/wasn’t accepted/)
    expect(assignErrorMessage(null)).toMatch(/could not be assigned/)
  })
})

describe('latestCase', () => {
  it('returns the most recently opened case', () => {
    const cases = [
      { id: 'old', createdAt: '2026-08-01T09:00:00Z' },
      { id: 'new', createdAt: '2026-09-15T09:00:00Z' },
    ]
    expect(latestCase(cases).id).toBe('new')
  })

  it('returns null when the client has no case', () => {
    expect(latestCase([])).toBeNull()
  })
})

describe('stagePath and stageBody', () => {
  it('builds the stage path for one case', () => {
    expect(stagePath('abc')).toBe('/api/cases/abc/stage')
  })

  it('sends only the stage name', () => {
    expect(stageBody('UnderReview')).toEqual({ stage: 'UnderReview' })
  })
})

describe('canChangeStage', () => {
  it('allows open cases and blocks archived ones', () => {
    expect(canChangeStage({ isArchived: false })).toBe(true)
    expect(canChangeStage({ isArchived: true })).toBe(false)
    expect(canChangeStage(null)).toBe(false)
  })
})

describe('stageChangeError', () => {
  it('requires a stage to be chosen', () => {
    expect(stageChangeError('InitialIntake', '')).toMatch(/choose/i)
  })

  it('rejects the stage the case is already at, by name or index', () => {
    expect(stageChangeError('UnderReview', 'UnderReview')).toMatch(/already/i)
    expect(stageChangeError(2, 'UnderReview')).toMatch(/already/i)
  })

  it('accepts a different stage, forwards or back', () => {
    expect(stageChangeError('InitialIntake', 'DocumentGathering')).toBeNull()
    expect(stageChangeError('FinalDecision', 'UnderReview')).toBeNull()
  })
})

describe('stageActionLabel', () => {
  it('names a forward move', () => {
    expect(stageActionLabel('InitialIntake', 'DocumentGathering')).toBe('Move to document gathering')
  })

  it('says when the move goes back', () => {
    expect(stageActionLabel('FinalDecision', 'UnderReview')).toBe('Move back to under review')
    expect(stageActionLabel(4, 'InitialIntake')).toBe('Move back to initial intake')
  })

  it('falls back when nothing is chosen', () => {
    expect(stageActionLabel('InitialIntake', '')).toBe('Update stage')
  })
})

describe('stageErrorMessage', () => {
  it('explains a case that is not assigned to the consultant', () => {
    expect(stageErrorMessage({ status: 403 })).toMatch(/assigned/i)
  })

  it('replaces the raw validation body of a 400', () => {
    expect(stageErrorMessage({ status: 400, message: 'One or more validation errors occurred.' })).not.toMatch(
      /validation/i,
    )
  })

  it('keeps other API messages and has a fallback', () => {
    expect(stageErrorMessage({ status: 500, message: 'Server down' })).toBe('Server down')
    expect(stageErrorMessage(undefined)).toMatch(/could not be saved/i)
  })
})
