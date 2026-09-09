import type { PublicOrgDto } from '@/features/public/types/public.types'

export interface PublicoOutletContext {
  org: PublicOrgDto
  slug: string
}
