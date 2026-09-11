/**
 * Arma la línea de ubicación de una publicación ("Barrio, Localidad").
 *
 * El importador de Tokko dejó `city` contaminada en parte de la cartera: trae la
 * localidad pegada a la dirección ("San Miguel Belgrano al 1800"), y como el
 * barrio suele repetir esa misma localidad el sitio termina mostrando
 * "San Miguel, San Miguel Belgrano al 1800". Acá se corrige lo que se puede
 * ver: si una parte ya contiene a la otra, se muestra una sola vez.
 *
 * Es una defensa de presentación, no el arreglo de fondo: los datos se sanean
 * en la base con `tools/sanear-ciudades`.
 */

/** Compara ignorando mayúsculas y tildes ("Lujan" ≡ "Luján"). */
function equalLoose(a: string, b: string): boolean {
  return a.localeCompare(b, 'es-AR', { sensitivity: 'base' }) === 0
}

/** `whole` empieza con la palabra completa `prefix` (seguida de un espacio). */
function startsWithWord(whole: string, prefix: string): boolean {
  return whole.length > prefix.length
    && whole[prefix.length] === ' '
    && equalLoose(whole.slice(0, prefix.length), prefix)
}

/** `whole` termina con la palabra completa `suffix` (precedida de un espacio). */
function endsWithWord(whole: string, suffix: string): boolean {
  const at = whole.length - suffix.length
  return at > 1
    && whole[at - 1] === ' '
    && equalLoose(whole.slice(at), suffix)
}

export function locationLine(neighborhood: string | null, city: string | null): string {
  const barrio = neighborhood?.trim() ?? ''
  const ciudad = city?.trim() ?? ''

  if (!barrio) return ciudad
  if (!ciudad) return barrio

  // La ciudad es el barrio, o el barrio con la dirección pegada detrás.
  if (equalLoose(ciudad, barrio) || startsWithWord(ciudad, barrio)) return barrio
  // El barrio ya contiene a la ciudad ("Centro De Lujan" / "Lujan").
  if (startsWithWord(barrio, ciudad) || endsWithWord(barrio, ciudad)) return barrio

  return `${barrio}, ${ciudad}`
}
