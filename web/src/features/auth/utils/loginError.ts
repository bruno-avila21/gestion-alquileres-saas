import { isAxiosError } from 'axios'

/**
 * Traduce el fallo de un login a algo que el usuario pueda accionar.
 *
 * Hasta ahora la pantalla mostraba "Credenciales inválidas" ante *cualquier*
 * error, incluido el servidor caído o la falta de conexión, y mandaba a la
 * persona a probar contraseñas cuando el problema estaba en otro lado.
 *
 * El API responde 401 con el mismo texto para contraseña incorrecta y para
 * organización inexistente, y eso está bien: decir cuál de las dos falló
 * revelaría qué inmobiliarias existen. Por eso el mensaje del 401 nombra los
 * tres campos — es la pista honesta más precisa que se puede dar.
 */
export function loginErrorMessage(error: unknown): string {
  if (!isAxiosError(error)) return 'No pudimos ingresar. Probá de nuevo en unos minutos.'

  if (!error.response) {
    return error.code === 'ECONNABORTED'
      ? 'El servidor tardó demasiado en responder. Probá de nuevo.'
      : 'No pudimos conectarnos con el servidor. Revisá tu conexión a internet.'
  }

  const { status, data } = error.response

  if (status === 401) return 'Revisá la organización, el email y la contraseña: alguno no coincide.'
  if (status === 403) return 'Tu usuario no tiene permiso para entrar a este portal.'
  if (status === 429) return 'Demasiados intentos seguidos. Esperá un minuto y volvé a probar.'
  if (status >= 500) return 'El servidor tuvo un problema. Probá de nuevo en unos minutos.'

  if (status === 400) {
    // ProblemDetails de ASP.NET: { errors: { Campo: ["mensaje", …] } }
    const errors = (data as { errors?: Record<string, string[]> } | undefined)?.errors
    const first = errors && Object.values(errors).flat()[0]
    if (first) return first
    const message = (data as { error?: string } | undefined)?.error
    if (message) return message
  }

  return 'No pudimos ingresar. Probá de nuevo en unos minutos.'
}
