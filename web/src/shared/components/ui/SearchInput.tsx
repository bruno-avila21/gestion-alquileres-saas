import { IcSearch } from './Icons'

interface SearchInputProps {
  value: string
  onChange: (value: string) => void
  placeholder: string
  /** Etiqueta accesible. Por defecto usa el placeholder, que no alcanza como nombre accesible. */
  ariaLabel?: string
  /** Ancho en px. El de por defecto entra cómodo en cualquier barra de filtros del panel. */
  width?: number
  /** Ocupa el espacio disponible en vez de un ancho fijo, para las barras de filtro. */
  grow?: boolean
  className?: string
}

/**
 * El buscador de las listas del panel.
 *
 * Existía siete veces, de tres formas distintas: unas como `<input className="input input--sm">`
 * pelado sin lupa, otras como un div armado a mano con el ícono adentro y el input sin borde, y
 * con tres anchos diferentes (220, 280, 320). Hacen exactamente el mismo trabajo y se veían
 * distinto según en qué pantalla estuvieras parado.
 */
export function SearchInput({
  value, onChange, placeholder, ariaLabel, width = 300, grow, className,
}: SearchInputProps) {
  return (
    <div
      className={`row${className ? ` ${className}` : ''}`}
      style={{
        gap: 8,
        ...(grow ? { flex: 1, minWidth: 0 } : { width }),
        maxWidth: '100%',
        height: 'var(--input-h)',
        padding: '0 10px',
        background: 'var(--surface)',
        border: '1px solid var(--hairline-2)',
        borderRadius: 'var(--r-3)',
      }}
    >
      <IcSearch size={14} style={{ color: 'var(--muted)', flexShrink: 0 }} />
      <input
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        aria-label={ariaLabel ?? placeholder}
        style={{
          border: 'none', outline: 'none', background: 'transparent',
          width: '100%', fontSize: 'var(--fs-sm)', color: 'var(--ink)',
        }}
      />
    </div>
  )
}
