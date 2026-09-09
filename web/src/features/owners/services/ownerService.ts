import { api } from '@/shared/lib/api'
import { filenameFromContentDisposition } from '@/shared/lib/downloadFile'
import type { OwnerDto, OwnerSettlementDto } from '../types/owner.types'

export const ownerService = {
  async list(): Promise<OwnerDto[]> {
    const { data } = await api.get<OwnerDto[]>('/owners')
    return data
  },
  async getSettlement(ownerId: string, from: string, to: string): Promise<OwnerSettlementDto> {
    const { data } = await api.get<OwnerSettlementDto>(`/owners/${ownerId}/settlement`, {
      params: { from, to },
    })
    return data
  },
  async downloadSettlementPdf(ownerId: string, from: string, to: string): Promise<{ blob: Blob; fileName: string }> {
    const res = await api.get<Blob>(`/owners/${ownerId}/settlement/pdf`, {
      params: { from, to },
      responseType: 'blob',
    })
    return {
      blob: res.data,
      fileName: filenameFromContentDisposition(res.headers['content-disposition']) ?? 'liquidacion.pdf',
    }
  },
}
