export interface DocumentDto {
  id: string
  contractId: string
  fileName: string
  mimeType: string
  sizeBytes: number
  createdAt: string
}

export interface DocumentDownloadUrlDto {
  token: string
  url: string
  expiresAt: string
}
