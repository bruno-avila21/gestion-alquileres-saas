import { api } from '@/shared/lib/api'
import type { DocumentDto, DocumentDownloadUrlDto } from '../types/document.types'
import type { PagedResult } from '@/shared/types/paged'

export const documentService = {
  async list(contractId: string): Promise<DocumentDto[]> {
    const { data } = await api.get<DocumentDto[]>(`/contracts/${contractId}/documents`)
    return data
  },

  async upload(contractId: string, file: File, isVisibleToTenant = false): Promise<DocumentDto> {
    const form = new FormData()
    form.append('file', file)
    // Secure by default (audit A2): documents are private to staff until explicitly shared.
    form.append('isVisibleToTenant', String(isVisibleToTenant))
    const { data } = await api.post<DocumentDto>(`/contracts/${contractId}/documents`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },

  async setVisibility(contractId: string, docId: string, isVisibleToTenant: boolean): Promise<DocumentDto> {
    const { data } = await api.patch<DocumentDto>(
      `/contracts/${contractId}/documents/${docId}/visibility`,
      { isVisibleToTenant },
    )
    return data
  },

  async getDownloadUrl(contractId: string, docId: string): Promise<DocumentDownloadUrlDto> {
    const { data } = await api.get<DocumentDownloadUrlDto>(
      `/contracts/${contractId}/documents/${docId}/download-url`
    )
    return data
  },

  async deleteDoc(contractId: string, docId: string): Promise<void> {
    await api.delete(`/contracts/${contractId}/documents/${docId}`)
  },

  async listMine(): Promise<DocumentDto[]> {
    const { data } = await api.get<DocumentDto[]>('/me/documents')
    return data
  },
  async listAll(params: { page: number; pageSize: number; search?: string }): Promise<PagedResult<DocumentDto>> {
    const { data } = await api.get<PagedResult<DocumentDto>>('/documents', { params })
    return data
  },
}
