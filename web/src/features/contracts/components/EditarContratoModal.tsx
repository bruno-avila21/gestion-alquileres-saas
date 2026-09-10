import { useState } from 'react'
import { useUpdateContract } from '../hooks/useContracts'
import { useProperties } from '@/features/properties/hooks/useProperties'
import { useAppTenants } from '@/features/apptenants/hooks/useAppTenants'
import { ContratoFormFields } from './ContratoFormFields'
import {
  contractFormFromDto, contractFormToRequest, type ContractFormState,
} from '../utils/contractForm'
import type { ContractDto } from '../types/contract.types'

/**
 * Edición de un contrato ya cargado.
 *
 * El endpoint `PUT /contracts/{id}` y `useUpdateContract` existían desde hacía meses, pero
 * ninguna pantalla los usaba: el botón "Editar" de la ficha no tenía `onClick`. Para
 * corregir el alquiler, la indexación o el punitorio de un contrato había que ir por la
 * API — y eso se nota apenas se pactan punitorios sobre una cartera ya cargada.
 *
 * Usa los mismos campos que el alta (`ContratoFormFields`) porque es la misma entidad con
 * las mismas reglas; lo único propio de editar es de dónde sale el estado inicial y el
 * aviso de que hay cosas que el formulario no rehace.
 */
export function EditarContratoModal({
  contract,
  onClose,
}: {
  contract: ContractDto | null
  onClose: () => void
}) {
  const { data: properties } = useProperties()
  const { data: tenants } = useAppTenants()
  const update = useUpdateContract()

  // La clave remonta el estado cuando cambia el contrato: sin esto, abrir la edición de otro
  // contrato reusaría el formulario cargado con los datos del anterior.
  return contract ? (
    <Formulario
      key={contract.id}
      contract={contract}
      properties={properties}
      tenants={tenants}
      update={update}
      onClose={onClose}
    />
  ) : null
}

function Formulario({
  contract, properties, tenants, update, onClose,
}: {
  contract: ContractDto
  properties: ReturnType<typeof useProperties>['data']
  tenants: ReturnType<typeof useAppTenants>['data']
  update: ReturnType<typeof useUpdateContract>
  onClose: () => void
}) {
  const [form, setForm] = useState<ContractFormState>(() => contractFormFromDto(contract))
  const [error, setError] = useState('')

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    try {
      await update.mutateAsync({ id: contract.id, req: contractFormToRequest(form) })
      onClose()
    } catch (err) {
      // El backend devuelve el motivo real —solapamiento de períodos, propiedad de otra
      // organización, contrato rescindido— y es más útil que un texto genérico.
      const detalle =
        (err as { response?: { data?: { error?: string; message?: string } } })?.response?.data
      setError(detalle?.error || detalle?.message || 'No se pudo guardar. Verificá los datos.')
    }
  }

  return (
    <div className="modal-backdrop" role="dialog" aria-modal="true" aria-label="Editar contrato">
      <div className="modal" style={{ maxWidth: 720 }}>
        <div className="modal-h">
          <h3>Editar contrato</h3>
          <button className="btn btn--ghost btn--icon btn--sm" onClick={onClose} aria-label="Cerrar">✕</button>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="modal-b" style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', lineHeight: 1.5 }}>
              Cambia las condiciones pactadas de acá en adelante. Los ajustes y las
              transacciones ya registradas no se recalculan.
            </div>

            {error && (
              <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>{error}</div>
            )}

            <ContratoFormFields
              form={form}
              setForm={setForm}
              properties={properties}
              tenants={tenants}
              idPrefix="edit-"
            />

            <div className="row" style={{ gap: 8, justifyContent: 'flex-end' }}>
              <button type="button" className="btn btn--sm" onClick={onClose}>Cancelar</button>
              <button type="submit" className="btn btn--sm btn--primary" disabled={update.isPending}>
                {update.isPending ? 'Guardando…' : 'Guardar cambios'}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  )
}
