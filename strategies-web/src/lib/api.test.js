import { describe, expect, it } from 'vitest'
import { filenameFromDisposition } from './api'

describe('filenameFromDisposition', () => {
  it('reads the filename the API currently sends for the cases export', () => {
    const header = "attachment; filename=cases.csv; filename*=UTF-8''cases.csv"
    expect(filenameFromDisposition(header, 'fallback.csv')).toBe('cases.csv')
  })

  it('decodes UTF-8 filenames and reads quoted plain filenames', () => {
    expect(filenameFromDisposition("attachment; filename*=UTF-8''case%20report.csv", 'x')).toBe('case report.csv')
    expect(filenameFromDisposition('attachment; filename="leads 2026.csv"', 'x')).toBe('leads 2026.csv')
  })

  it('uses the fallback when there is no usable header', () => {
    expect(filenameFromDisposition(null, 'fallback.csv')).toBe('fallback.csv')
    expect(filenameFromDisposition('attachment', 'fallback.csv')).toBe('fallback.csv')
  })
})
