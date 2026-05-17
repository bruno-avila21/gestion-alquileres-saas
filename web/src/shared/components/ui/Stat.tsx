import type { ReactNode } from 'react'

interface StatProps {
  label: string
  value: string
  delta?: string
  deltaUp?: boolean | null
  icon?: ReactNode
  iconColor?: string
  sparkline?: ReactNode
}

export function Stat({ label, value, delta, deltaUp, icon, iconColor, sparkline }: StatProps) {
  return (
    <div className="card stat">
      <div className="between">
        <span className="lbl">{label}</span>
        {icon && <span style={{ color: iconColor, opacity: 0.8 }}>{icon}</span>}
      </div>
      <div className="val">{value}</div>
      {(delta !== undefined || sparkline !== undefined) && (
        <div className="between" style={{ marginTop: 8 }}>
          {delta !== undefined && (
            deltaUp === null
              ? <span style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)' }}>{delta}</span>
              : <span className={`delta-pill ${deltaUp ? 'up' : 'down'}`}>{delta}</span>
          )}
          {sparkline}
        </div>
      )}
    </div>
  )
}
