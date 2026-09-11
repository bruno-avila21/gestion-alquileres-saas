import type { PublicOperationType, PublicPropertyType } from '../types/public.types'

export const OPERATION_LABELS: Record<PublicOperationType, string> = {
  Sale: 'Venta',
  Rent: 'Alquiler',
  TemporaryRent: 'Alquiler temporario',
}

export const PROPERTY_TYPE_LABELS: Record<PublicPropertyType, string> = {
  House: 'Casa',
  Apartment: 'Departamento',
  PH: 'PH',
  Land: 'Terreno',
  Commercial: 'Local',
  Office: 'Oficina',
  Other: 'Otro',
}

export const SORT_LABELS: Record<string, string> = {
  '': 'Destacadas',
  price_asc: 'Precio ↑',
  price_desc: 'Precio ↓',
  rooms_desc: 'Más ambientes',
  rooms_asc: 'Menos ambientes',
  newest: 'Más recientes',
}

/** Devuelve el label español de una operación, o el valor crudo si no se reconoce. */
export function operationLabel(op: string): string {
  return OPERATION_LABELS[op as PublicOperationType] ?? op
}

/** Devuelve el label español de un tipo de propiedad, o el valor crudo si no se reconoce. */
export function propertyTypeLabel(type: string): string {
  return PROPERTY_TYPE_LABELS[type as PublicPropertyType] ?? type
}

const currencyFormatter = new Intl.NumberFormat('es-AR')

/** Precio con prefijo de moneda (`US$`/`$`), formateado con separador de miles es-AR. */
export function formatPublicPrice(amount: number, currency: 'ARS' | 'USD'): string {
  const prefix = currency === 'USD' ? 'US$' : '$'
  return `${prefix}${currencyFormatter.format(amount)}`
}

export function formatArea(m2: number): string {
  return `${currencyFormatter.format(m2)} m²`
}
