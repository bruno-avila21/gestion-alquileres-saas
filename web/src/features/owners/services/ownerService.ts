import { api } from '@/shared/lib/api'
import { filenameFromContentDisposition } from '@/shared/lib/downloadFile'
import type {
  AdjustmentProjection, OwnerDto, OwnerSettlementDto, SimulateAdjustmentParams,
} from '../types/owner.types'

export const ownerService = {
  async list(): Promise<OwnerDto[]> {
    const { data } = await api.get<OwnerDto[]>('/owners')
    return data
  },
  /** La liquidación del período para todos los propietarios con cobranzas: la vista de lote. */
  async listSettlements(from: string, to: string): Promise<OwnerSettlementDto[]> {
    const { data } = await api.get<OwnerSettlementDto[]>('/owners/settlements', { params: { from, to } })
    return data
  },
  /**
   * Simulación de ajuste sobre parámetros sueltos, sin contrato. La API responde 204 para
   * tipo Manual, que no tiene fórmula; axios entrega un cuerpo vacío y acá se vuelve null.
   */
  async simulateAdjustment(params: SimulateAdjustmentParams): Promise<AdjustmentProjection | null> {
    const { data } = await api.get<AdjustmentProjection | ''>('/rent-adjustments/simulate', { params })
    return data || null
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
