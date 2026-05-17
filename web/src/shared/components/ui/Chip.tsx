import type { ReactNode } from 'react'

interface ChipProps {
  variant?: 'default' | 'ok' | 'warn' | 'danger' | 'info' | 'icl' | 'ipc' | 'solid'
  dot?: boolean
  children: ReactNode
  style?: React.CSSProperties
  className?: string
}

export function Chip({ variant = 'default', dot = false, children, style, className }: ChipProps) {
  const variantClass = variant !== 'default' ? ` chip--${variant}` : ''
  return (
    <span className={`chip${variantClass}${className ? ` ${className}` : ''}`} style={style}>
      {dot && <span className="dot" />}
      {children}
    </span>
  )
}
