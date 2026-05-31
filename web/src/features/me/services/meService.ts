import { api } from '@/shared/lib/api'
import type { ContractDto, RentHistoryDto, TransactionDto } from '@/features/contracts/types/contract.types'

export const meService = {
  async getContract(): Promise<ContractDto> {
    const { data } = await api.get<ContractDto>('/me/contract')
    return data
  },
  async getTransactions(): Promise<TransactionDto[]> {
    const { data } = await api.get<TransactionDto[]>('/me/transactions')
    return data
  },
  async getRentHistory(): Promise<RentHistoryDto[]> {
    const { data } = await api.get<RentHistoryDto[]>('/me/history')
    return data
  },
}
