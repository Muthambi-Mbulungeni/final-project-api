import { describe, expect, it } from 'vitest'
import { actionLabel, actorName } from './audit'

describe('actionLabel', () => {
  it('turns the API code names into the same sentences the rest of the log uses', () => {
    expect(actionLabel('UpdateLeadStatus')).toBe('Reviewed lead')
    expect(actionLabel('UpdateCaseStage')).toBe('Changed case stage')
    expect(actionLabel('ArchiveDocument')).toBe('Archived document')
  })

  it('splits unknown code names into words and leaves sentences alone', () => {
    expect(actionLabel('SuspendUser')).toBe('Suspend user')
    expect(actionLabel('Advanced case stage')).toBe('Advanced case stage')
    expect(actionLabel(' Rejected document ')).toBe('Rejected document')
    expect(actionLabel('')).toBe('—')
    expect(actionLabel(null)).toBe('—')
  })
})

describe('actorName', () => {
  it('returns the trimmed full name', () => {
    expect(actorName({ userName: '  Thandi Nkosi ' })).toBe('Thandi Nkosi')
  })

  it('returns null for the API placeholder', () => {
    expect(actorName({ userName: 'System/Unknown' })).toBeNull()
  })

  it('returns null when the name is missing, blank or not a string', () => {
    expect(actorName({})).toBeNull()
    expect(actorName({ userName: '   ' })).toBeNull()
    expect(actorName({ userName: 42 })).toBeNull()
    expect(actorName(null)).toBeNull()
  })
})
