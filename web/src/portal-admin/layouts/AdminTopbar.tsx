import type { ReactNode } from 'react'
import { IcChev } from '@/shared/components/ui/Icons'

interface AdminTopbarProps {
  crumbs: string[]
  right?: ReactNode
}

export function AdminTopbar({ crumbs, right }: AdminTopbarProps) {
  return (
    <div className="topbar">
      <div className="crumb grow">
        {crumbs.map((crumb, i) => (
          <span key={i} style={{ display: 'inline-flex', alignItems: 'center', gap: 6 }}>
            {i < crumbs.length - 1 ? (
              <>
                <span>{crumb}</span>
                <IcChev size={12} style={{ color: 'var(--muted)', opacity: 0.6 }} />
              </>
            ) : (
              <b>{crumb}</b>
            )}
          </span>
        ))}
      </div>
      {right && <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>{right}</div>}
    </div>
  )
}
