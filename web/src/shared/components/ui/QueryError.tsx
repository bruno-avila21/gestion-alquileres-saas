interface QueryErrorProps {
  /** Called when the user taps "Reintentar" — pass the query's refetch. */
  onRetry?: () => void
  message?: string
}

/**
 * Standard error state for a failed query (audit A6). Pages that only handled isLoading showed the
 * normal empty state ("Sin transacciones") on failure, so a network/500 error read as "no data".
 * Render this when `isError` is true instead of the empty state.
 */
export function QueryError({ onRetry, message }: QueryErrorProps) {
  return (
    <div
      role="alert"
      className="card"
      style={{ padding: '20px 14px', textAlign: 'center', color: 'var(--danger)', fontSize: 'var(--fs-sm)' }}
    >
      <div>{message ?? 'No pudimos cargar la información.'}</div>
      {onRetry && (
        <button className="btn btn--sm" style={{ marginTop: 10 }} onClick={onRetry}>
          Reintentar
        </button>
      )}
    </div>
  )
}
