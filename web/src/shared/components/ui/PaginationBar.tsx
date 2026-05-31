import { IcArrowL, IcArrowR } from './Icons'

interface Props {
  page: number
  totalPages: number
  total: number
  pageSize: number
  onPageChange: (page: number) => void
}

export function PaginationBar({ page, totalPages, total, pageSize, onPageChange }: Props) {
  const start = total === 0 ? 0 : page * pageSize + 1
  const end = Math.min((page + 1) * pageSize, total)
  return (
    <div
      className="between hairline-t"
      style={{ padding: '10px 14px', color: 'var(--muted)', fontSize: 'var(--fs-xs)' }}
    >
      <span>
        {total === 0 ? 'Sin resultados' : `Mostrando ${start}–${end} de ${total}`}
      </span>
      {totalPages > 1 && (
        <div className="row" style={{ gap: 4 }}>
          <button
            className="btn btn--ghost btn--sm"
            disabled={page === 0}
            onClick={() => onPageChange(page - 1)}
          >
            <IcArrowL size={12} />
          </button>
          <span style={{ padding: '0 6px' }}>{page + 1} / {totalPages}</span>
          <button
            className="btn btn--ghost btn--sm"
            disabled={page >= totalPages - 1}
            onClick={() => onPageChange(page + 1)}
          >
            <IcArrowR size={12} />
          </button>
        </div>
      )}
    </div>
  )
}
