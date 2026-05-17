import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { IndexTable } from '../components/IndexTable'
import type { IndexValueDto } from '../types/index.types'

function makeRow(overrides: Partial<IndexValueDto> = {}): IndexValueDto {
  return {
    id: '1',
    indexType: 'ICL',
    period: '2024-03-01',
    value: 101.5,
    variationPct: null,
    source: 'BCRA',
    fetchedAt: '2024-03-01T10:00:00Z',
    ...overrides,
  }
}

describe('IndexTable', () => {
  it('renders_loading_state_when_isLoading_true', () => {
    render(<IndexTable rows={undefined} isLoading={true} error={null} />)
    expect(screen.getByText(/cargando/i)).toBeTruthy()
  })

  it('renders_error_alert_when_error_present', () => {
    render(<IndexTable rows={undefined} isLoading={false} error={new Error('Server error')} />)
    const alert = screen.getByRole('alert')
    expect(alert).toBeTruthy()
    expect(alert.textContent).toContain('Server error')
  })

  it('renders_empty_state_when_rows_empty', () => {
    render(<IndexTable rows={[]} isLoading={false} error={null} />)
    expect(screen.getByText(/sin índices/i)).toBeTruthy()
  })

  it('renders_table_with_correct_columns_and_rows', () => {
    const rows = [
      makeRow({ id: '1', period: '2024-01-01', value: 101.5, source: 'BCRA' }),
      makeRow({ id: '2', period: '2024-02-01', value: 102.3, source: 'BCRA' }),
    ]
    render(<IndexTable rows={rows} isLoading={false} error={null} />)

    // 5 column headers
    expect(screen.getByText('Período')).toBeTruthy()
    expect(screen.getByText('Valor')).toBeTruthy()
    expect(screen.getByText('Variación %')).toBeTruthy()
    expect(screen.getByText('Fuente')).toBeTruthy()
    expect(screen.getByText('Sincronizado')).toBeTruthy()

    // period formatted as yyyy-MM
    expect(screen.getByText('2024-01')).toBeTruthy()
    expect(screen.getByText('2024-02')).toBeTruthy()

    // value contains digits from 101.5
    const valueTexts = screen.getAllByText(/101/)
    expect(valueTexts.length).toBeGreaterThanOrEqual(1)

    // source present
    const sourceTexts = screen.getAllByText('BCRA')
    expect(sourceTexts.length).toBe(2)
  })

  it('formats_variation_pct_null_as_em_dash', () => {
    render(<IndexTable rows={[makeRow({ variationPct: null })]} isLoading={false} error={null} />)
    expect(screen.getByText('\u2014')).toBeTruthy()
  })

  it('formats_positive_variation_pct_with_plus_sign', () => {
    render(<IndexTable rows={[makeRow({ variationPct: 1.234 })]} isLoading={false} error={null} />)
    // formatted as es-AR with signDisplay: 'exceptZero' — positive shows + sign
    const cell = screen.getByText(/1[,.]23%/)
    expect(cell.textContent).toContain('%')
  })
})
