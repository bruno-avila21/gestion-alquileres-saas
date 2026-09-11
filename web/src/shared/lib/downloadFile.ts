/**
 * Extrae el nombre de archivo de un header `Content-Disposition` tipo
 * `attachment; filename="recibo-REC-00000042.pdf"`. Devuelve `null` si no viene o no matchea.
 */
export function filenameFromContentDisposition(header: string | undefined | null): string | null {
  if (!header) return null
  const match = /filename="?([^";]+)"?/i.exec(header)
  return match ? match[1].trim() : null
}

/** Dispara la descarga de un blob con el mismo patrón que ya usa la exportación CSV del panel. */
export function downloadBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.click()
  URL.revokeObjectURL(url)
}
