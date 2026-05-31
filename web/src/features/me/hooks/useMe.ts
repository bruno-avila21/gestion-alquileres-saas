import { useQuery } from '@tanstack/react-query'
import { meService } from '../services/meService'

export function useMyContract() {
  return useQuery({
    queryKey: ['me', 'contract'],
    queryFn: () => meService.getContract(),
    retry: false,
  })
}

export function useMyTransactions() {
  return useQuery({
    queryKey: ['me', 'transactions'],
    queryFn: () => meService.getTransactions(),
    retry: false,
  })
}

export function useMyRentHistory() {
  return useQuery({
    queryKey: ['me', 'history'],
    queryFn: () => meService.getRentHistory(),
    retry: false,
  })
}
