import { describe, it, expect, vi, beforeEach } from 'vitest'
import { renderHook, waitFor, act } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { PropsWithChildren } from 'react'
import { useSyncIndex } from '../hooks/useSyncIndex'
import { indexService } from '../services/indexService'

vi.mock('../services/indexService')

function wrapper(qc: QueryClient) {
  return ({ children }: PropsWithChildren) => (
    <QueryClientProvider client={qc}>{children}</QueryClientProvider>
  )
}

describe('useSyncIndex', () => {
  beforeEach(() => vi.clearAllMocks())

  it('invalidates indexes query on success', async () => {
    const qc = new QueryClient({ defaultOptions: { queries: { retry: false } } })
    const invalidateSpy = vi.spyOn(qc, 'invalidateQueries')
    vi.mocked(indexService.sync).mockResolvedValue({
      success: true,
      wasFallback: false,
      alreadyExisted: false,
      message: null,
      indexValue: {
        id: '1',
        indexType: 'ICL',
        period: '2024-03-01',
        value: 100,
        variationPct: null,
        source: 'BCRA',
        fetchedAt: '2024-03-01T00:00:00Z',
      },
    })
    const { result } = renderHook(() => useSyncIndex(), { wrapper: wrapper(qc) })
    await act(async () => {
      result.current.mutate({ indexType: 'ICL', period: '2024-03-01' })
    })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['indexes'] })
  })

  it('surfaces error when server returns 409', async () => {
    const qc = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    })
    vi.mocked(indexService.sync).mockRejectedValue(
      new Error('Request failed with status code 409'),
    )
    const { result } = renderHook(() => useSyncIndex(), { wrapper: wrapper(qc) })
    await act(async () => {
      result.current.mutate({ indexType: 'ICL', period: '2099-01-01' })
    })
    await waitFor(() => expect(result.current.isError).toBe(true))
  })
})
