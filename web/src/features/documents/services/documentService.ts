import { api } from '@/shared/lib/api'
import type { DocumentDto, DocumentDownloadUrlDto } from '../types/document.types'

export const documentService = {
  async list(contractId: string): Promise<DocumentDto[]> {
    const { data } = await api.get<DocumentDto[]>(`/contracts/${contractId}/documents`)
    return data
  },

  async upload(contractId: string, file: File): Promise<DocumentDto> {
    const form = new FormData()
    form.append('file', file)
    const { data } = await api.post<DocumentDto>(`/contracts/${contractId}/documents`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
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
  async listAll(): Promise<DocumentDto[]> {
    const { data } = await api.get<DocumentDto[]>('/documents')
    return data
  },
}
