import { describe, expect, it } from 'vitest'
import { actorName } from './audit'

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
