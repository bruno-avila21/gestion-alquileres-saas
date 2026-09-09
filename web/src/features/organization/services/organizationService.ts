import { api } from '@/shared/lib/api'
import { isAxiosError } from 'axios'
import type { OrganizationDto, UpdateOrganizationRequest } from '../types/organization.types'

export const organizationService = {
  async get(): Promise<OrganizationDto> {
    const { data } = await api.get<OrganizationDto>('/organization')
    return data
  },
  async update(req: UpdateOrganizationRequest): Promise<OrganizationDto> {
    const { data } = await api.put<OrganizationDto>('/organization', req)
    return data
  },
  async uploadLogo(file: File): Promise<OrganizationDto> {
    const form = new FormData()
    form.append('file', file)
    const { data } = await api.post<OrganizationDto>('/organization/logo', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return data
  },
  async deleteLogo(): Promise<void> {
    await api.delete('/organization/logo')
  },
  /**
   * Trae el logo como blob y lo devuelve como object URL, listo para un <img src>. Se pide con la
   * cookie de sesión (igual que cualquier otro endpoint privado), nunca como URL directa al storage.
   * `null` si la organización no tiene logo cargado (404).
   */
  async logoUrl(): Promise<string | null> {
    try {
      const { data } = await api.get<Blob>('/organization/logo', { responseType: 'blob' })
      return URL.createObjectURL(data)
    } catch (err) {
      if (isAxiosError(err) && err.response?.status === 404) return null
      throw err
    }
  },
}
