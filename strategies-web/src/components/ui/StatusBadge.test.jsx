// @vitest-environment jsdom
import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import '../../test/dom'
import StatusBadge from './StatusBadge'

describe('StatusBadge', () => {
  it('takes its tone from the enum', () => {
    render(<StatusBadge type="documentStatus" value="Rejected" />)
    expect(screen.getByText('Rejected').className).toContain('bg-brand-50')
  })

  it('accepts a tone and label for a status that is not a backend enum', () => {
    render(
      <>
        <StatusBadge tone="danger" label="Suspended" />
        <StatusBadge tone="success" label="Active" />
      </>,
    )
    expect(screen.getByText('Suspended').className).toContain('bg-brand-50 text-brand-700')
    expect(screen.getByText('Active').className).toContain('bg-emerald-50 text-emerald-800')
  })
})
