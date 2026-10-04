import { describe, expect, it } from 'vitest'
import {
  canChangeStage,
  latestCase,
  stageActionLabel,
  stageBody,
  stageChangeError,
  stageErrorMessage,
  stagePath,
} from './cases'

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
