import { useState } from 'react'
import { isAxiosError } from 'axios'
import { useChangePassword } from '../hooks/useChangePassword'

const MIN_LENGTH = 12

interface Props {
  /** Se muestra cuando el cambio es obligatorio y no una decisión del usuario. */
  forced?: boolean
  onDone: () => void
}

/**
 * Cambio de contraseña, compartido por los dos portales.
 *
 * Se usa tanto para el cambio voluntario como para el forzado del primer ingreso, cuando la
 * credencial la generó el sistema y viajó por WhatsApp o email.
 */
export function ChangePasswordForm({ forced = false, onDone }: Props) {
  const change = useChangePassword()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [repeat, setRepeat] = useState('')
  const [error, setError] = useState('')

  const tooShort = newPassword.length > 0 && newPassword.length < MIN_LENGTH
  const mismatch = repeat.length > 0 && repeat !== newPassword
  const canSubmit =
    currentPassword.length > 0 &&
    newPassword.length >= MIN_LENGTH &&
    repeat === newPassword &&
    !change.isPending

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setError('')
    try {
      await change.mutateAsync({ currentPassword, newPassword })
      onDone()
    } catch (err) {
      // El backend responde 409 cuando la contraseña actual no coincide y 400 cuando la nueva no
      // cumple la política. Distinguirlos evita el clásico "algo salió mal".
      if (isAxiosError(err) && err.response?.status === 409) {
        setError('La contraseña actual no es correcta.')
      } else if (isAxiosError(err) && err.response?.status === 400) {
        setError(`La contraseña nueva no cumple los requisitos: al menos ${MIN_LENGTH} caracteres y distinta de la actual.`)
      } else {
        setError('No pudimos cambiar la contraseña. Probá de nuevo en un momento.')
      }
    }
  }

  return (
    <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
      {forced && (
        <div className="card" style={{ padding: 14, background: 'var(--surface-2)', border: 'none' }}>
          <div style={{ fontWeight: 600, fontSize: 'var(--fs-sm)' }}>Elegí tu contraseña</div>
          <div style={{ fontSize: 'var(--fs-sm)', color: 'var(--muted)', marginTop: 4, lineHeight: 1.5 }}>
            La que te pasaron es temporal y quedó en esa conversación. Elegí una nueva para que sólo
            vos puedas entrar a tu cuenta.
          </div>
        </div>
      )}

      <div>
        <label className="label" htmlFor="currentPassword">
          {forced ? 'Contraseña temporal' : 'Contraseña actual'}
        </label>
        <input
          id="currentPassword"
          className="input"
          type="password"
          autoComplete="current-password"
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
          required
        />
      </div>

      <div>
        <label className="label" htmlFor="newPassword">Contraseña nueva</label>
        <input
          id="newPassword"
          className="input"
          type="password"
          autoComplete="new-password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          aria-describedby="newPasswordHelp"
          required
        />
        <div
          id="newPasswordHelp"
          style={{ fontSize: 'var(--fs-xs)', color: tooShort ? 'var(--danger)' : 'var(--muted)', marginTop: 4 }}
        >
          Al menos {MIN_LENGTH} caracteres.
        </div>
      </div>

      <div>
        <label className="label" htmlFor="repeatPassword">Repetir contraseña nueva</label>
        <input
          id="repeatPassword"
          className="input"
          type="password"
          autoComplete="new-password"
          value={repeat}
          onChange={(e) => setRepeat(e.target.value)}
          required
        />
        {mismatch && (
          <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--danger)', marginTop: 4 }}>
            Las dos contraseñas no coinciden.
          </div>
        )}
      </div>

      {error && (
        <div role="alert" style={{ fontSize: 'var(--fs-sm)', color: 'var(--danger)' }}>
          {error}
        </div>
      )}

      <button className="btn btn--primary" type="submit" disabled={!canSubmit}>
        {change.isPending ? 'Guardando…' : 'Guardar contraseña'}
      </button>

      <div style={{ fontSize: 'var(--fs-xs)', color: 'var(--muted)', lineHeight: 1.5 }}>
        Al cambiarla se cierran las sesiones abiertas en otros dispositivos.
      </div>
    </form>
  )
}
