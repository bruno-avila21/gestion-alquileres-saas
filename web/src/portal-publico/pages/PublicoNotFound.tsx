import { EmptySearchIcon } from '../components/icons'

export function PublicoNotFound({
  title = 'No encontramos este sitio',
  message = 'La inmobiliaria que buscás no existe o todavía no tiene su sitio publicado.',
}: {
  title?: string
  message?: string
}) {
  return (
    <div className="notfound" role="alert">
      <EmptySearchIcon size={48} />
      <h1>{title}</h1>
      <p>{message}</p>
    </div>
  )
}

export default PublicoNotFound
