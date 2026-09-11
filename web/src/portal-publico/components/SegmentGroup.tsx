import { useCallback, useLayoutEffect, useRef, useState } from 'react'

export interface SegmentOption<T extends string> {
  value: T
  label: string
}

interface SegmentGroupProps<T extends string> {
  options: SegmentOption<T>[]
  value: T
  onChange: (value: T) => void
  ariaLabel: string
  /**
   * `underline` — solapas de texto con un subrayado que se desliza, al modo del Pivot de
   * Fluent. Para la portada, donde el control es la entrada al buscador y no debe competir
   * con el botón "Buscar".
   * `pill` — pastilla rellena que se desliza por detrás de la opción elegida, al modo del
   * SegmentGroup de Chakra. Para el riel de filtros, donde hay que ver de un vistazo qué
   * está aplicado.
   */
  variant: 'underline' | 'pill'
  className?: string
}

/**
 * Grupo de segmentos con indicador deslizante.
 *
 * El indicador se posiciona midiendo el botón activo, no calculando "ancho / cantidad": los
 * rótulos tienen largos distintos ("Todas" contra "Temporario"), el riel de filtros los
 * acomoda en dos filas cuando no entran en una, y la inmobiliaria puede cambiar la
 * tipografía del sitio desde el panel. Cualquier fórmula fija se rompe con alguna de esas
 * tres cosas; medir el elemento real no.
 *
 * Las medidas viajan como variables CSS y no como estilos concretos para que cada variante
 * decida qué hacer con ellas — la pastilla usa alto y ancho, el subrayado sólo el ancho y se
 * queda pegado abajo.
 */
export function SegmentGroup<T extends string>({
  options, value, onChange, ariaLabel, variant, className,
}: SegmentGroupProps<T>) {
  const trackRef = useRef<HTMLDivElement>(null)
  const [box, setBox] = useState<{ x: number; y: number; w: number; h: number } | null>(null)

  const measure = useCallback(() => {
    const track = trackRef.current
    if (!track) return
    const active = track.querySelector<HTMLButtonElement>('button[data-active="true"]')
    if (!active) {
      setBox(null)
      return
    }
    setBox({ x: active.offsetLeft, y: active.offsetTop, w: active.offsetWidth, h: active.offsetHeight })
  }, [])

  useLayoutEffect(() => {
    measure()

    const track = trackRef.current
    if (!track || typeof ResizeObserver === 'undefined') return

    // Observa la pista Y cada botón: la fuente del sitio se carga después del primer pintado
    // (useSiteFonts), y al llegar cambia el ancho de los rótulos sin que cambie el de la pista.
    const observer = new ResizeObserver(measure)
    observer.observe(track)
    for (const button of track.querySelectorAll('button')) observer.observe(button)
    return () => observer.disconnect()
  }, [measure, options, value])

  const style = box
    ? ({
      '--segg-x': `${box.x}px`,
      '--segg-y': `${box.y}px`,
      '--segg-w': `${box.w}px`,
      '--segg-h': `${box.h}px`,
    } as React.CSSProperties)
    : undefined

  return (
    <div
      ref={trackRef}
      className={`segg segg--${variant}${box ? ' is-ready' : ''}${className ? ` ${className}` : ''}`}
      role="group"
      aria-label={ariaLabel}
      style={style}
    >
      <span className="segg-indicator" aria-hidden="true" />
      {options.map((option) => (
        <button
          key={option.value || '_all'}
          type="button"
          data-active={option.value === value}
          aria-pressed={option.value === value}
          onClick={() => onChange(option.value)}
        >
          {option.label}
        </button>
      ))}
    </div>
  )
}
