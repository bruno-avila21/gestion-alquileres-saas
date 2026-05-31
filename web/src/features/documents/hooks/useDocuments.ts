import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { documentService } from '../services/documentService'

export function useContractDocuments(contractId: string) {
  return useQuery({
    queryKey: ['documents', contractId],
    queryFn: () => documentService.list(contractId),
    enabled: !!contractId,
  })
}

export function useUploadDocument(contractId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (file: File) => documentService.upload(contractId, file),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['documents', contractId] }),
  })
}

export function useDeleteDocument(contractId: string) {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (docId: string) => documentService.deleteDoc(contractId, docId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['documents', contractId] }),
  })
}

export function useDocumentDownloadUrl(contractId: string, docId: string) {
  return useMutation({
    mutationFn: () => documentService.getDownloadUrl(contractId, docId),
  })
}

export function useMyDocuments() {
  return useQuery({
    queryKey: ['me', 'documents'],
    queryFn: () => documentService.listMine(),
    retry: false,
  })
}

export function useAllDocuments() {
  return useQuery({
    queryKey: ['documents', 'all'],
    queryFn: () => documentService.listAll(),
  })
}
