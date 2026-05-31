import type { TransactionDto } from '@/features/contracts/types/contract.types'

export interface DashboardDto {
  activeContractsCount: number
  monthlyRevenue: number
  expiringIn30DaysCount: number
  recentTransactions: TransactionDto[]
}
