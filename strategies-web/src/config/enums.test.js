import { describe, expect, it } from 'vitest'
import { enumFilter, enumOrder, resolveEnum } from './enums'

describe('resolveEnum', () => {
  it('resolves a value sent as its name', () => {
    expect(resolveEnum('caseStage', 'UnderReview')?.label).toBe('Under review')
  })

  it('resolves a value sent as its index, matching default .NET serialisation', () => {
    expect(resolveEnum('caseStage', 2)?.value).toBe('UnderReview')
  })

  it('returns null for unknown values, unknown enum types and missing input', () => {
    expect(resolveEnum('caseStage', 'Archived')).toBeNull()
    expect(resolveEnum('caseStage', 9)).toBeNull()
    expect(resolveEnum('notAnEnum', 'Pending')).toBeNull()
    expect(resolveEnum('caseStage', null)).toBeNull()
  })
})

describe('enumOrder', () => {
  it('returns the position of a value in the lifecycle', () => {
    expect(enumOrder('caseStage', 'InitialIntake')).toBe(0)
    expect(enumOrder('caseStage', 'FinalDecision')).toBe(4)
    expect(enumOrder('caseStage', 'Unknown')).toBeNull()
  })
})

describe('enumFilter', () => {
  it('builds options from the enum and normalises index values for filtering', () => {
    const filter = enumFilter('triageStatus', 'triageStatus', 'Triage')
    expect(filter.options.map((option) => option.value)).toEqual(['Pending', 'Qualified', 'Unqualified', 'NeedsReview'])
    expect(filter.getValue({ triageStatus: 3 })).toBe('NeedsReview')
    expect(filter.getValue({ triageStatus: 'Qualified' })).toBe('Qualified')
  })
})
