# BUGS

Bitácora de fallos de prueba y de pantalla. Una entrada por síntoma, escrita **antes**
de arreglar. El hook `qa-captura` deja el crudo en `.claude/qa/fallos.jsonl`.

---

### 1. `LoginForm` rompe los tests: "useSearchParams may be used only in the context of a Router"

- **Paso:** `cd web && pnpm test --run` → `src/features/auth/__tests__/LoginForm.test.tsx`,
  3 de 20 casos en rojo.
- **Error exacto:**
  ```
  ❯ useSearchParams .../react-router/dist/development/chunk-NXTEWSJO.js:715:47
  ❯ LoginForm src/features/auth/components/LoginForm.tsx:26:26
      26|   const [searchParams] = useSearchParams()
  ```
- **Reproducir:** renderizar `<LoginForm …/>` sin envolverlo en un router
  (`MemoryRouter`), que es exactamente lo que hace el test existente.
- **Causa:** al precargar el campo "Organización" desde `?org=` metí `useSearchParams()`
  dentro de `LoginForm`. El componente era presentacional y pasó a exigir un contexto de
  React Router que ninguno de sus usos —ni el test— garantiza. El dato que necesitaba es
  el valor *inicial* de un input no controlado: no hace falta suscribirse a los cambios
  de la URL para eso.
- **Arreglo:** leer `window.location.search` una sola vez en el `useState` inicial, sin
  hooks de router. `LoginForm` vuelve a ser independiente del árbol de rutas.

---

### 2. La ciudad de 30 propiedades trae la dirección pegada y se ve en todo el sitio público

- **Paso:** abrir https://web-production-dc836.up.railway.app/sitio/palavecino → cualquier
  tarjeta de Palermo, San Miguel o Capital Federal.
- **Error exacto:** la línea de ubicación dice
  `Palermo, Capital Federal Av Cordoba al 5800` en vez de `Palermo, Capital Federal`.
  30 de las 85 propiedades. Verificado también en el JSON de origen del scraper.
- **Reproducir:**
  ```
  curl -s ".../api/v1/public/palavecino/listings?pageSize=100" \
    | python -c "import sys,json,re; d=json.load(sys.stdin); \
        print(len([i for i in d['items'] if re.search(r'\d', i['city'] or '')]))"
  ```
- **Causa:** `tools/importar-sitio-tokko/scrape.py:190`. El `alt` de la foto trae la
  dirección al final, pero con **espacios dobles** donde `address` tiene uno solo
  (`"Av Cordoba  al 5800"` vs `"Av Cordoba al 5800"`). El `alt.endswith(card["address"])`
  daba `False`, la dirección nunca se recortaba, y el regex de `barrio_y_ciudad` la
  arrastraba dentro de la ciudad.
- **Arreglo:** comparar con los espacios normalizados (`re.sub(r"\s+", " ", …)`) antes del
  `endswith`. Para lo ya cargado, `tools/sanear-ciudades/sanear.py` recorta la dirección
  y reescribe las 30 por API (simulacro por defecto, `--aplicar` para escribir).
  Aplicado en producción: 30/30 corregidas, verificado con una segunda corrida que
  reporta "Nada que sanear".
- **Lección:** comparar texto scrapeado sin normalizar espacios es un falso negativo
  silencioso — no rompe nada, sólo ensucia los datos, y se descubre meses después en
  pantalla. Toda comparación de strings venidos de HTML va normalizada.

---

### 3. El panel muestra los punitorios como "Crédito"

- **Paso:** entrar a `/admin/dashboard` con la org `palavecino` → tabla "Transacciones
  recientes", primera fila (el punitorio de $ 10.500 de Ana López).
- **Error exacto:** el chip dice `Crédito` en vez de `Punitorio`.
- **Reproducir:** cualquier organización con una transacción de tipo `LateFee`.
- **Causa:** `DashboardPage.tsx` tenía dos fuentes de verdad para el mismo rótulo. Arriba
  el `Record<TransactionType, string>` **TX_LABEL**, completo y exhaustivo por tipo — que
  sólo se usaba para el CSV de exportación. Y adentro de la tabla, un ternario encadenado
  escrito a mano que terminaba en `: 'Crédito'`. Al sumar `LateFee` al enum se actualizó
  el Record (el compilador lo exige) pero no el ternario, que no tiene forma de exigirlo:
  el caso nuevo cayó en la rama final.
- **Arreglo:** la tabla usa `TX_LABEL[t.type]`. Queda un solo mapa, y `Record<TransactionType, …>`
  hace que agregar un tipo al enum rompa la compilación hasta rotularlo.
- **Lección:** un rótulo por enum va en un `Record<Enum, string>`, nunca en un ternario
  encadenado. El Record convierte "me olvidé un caso" en error de compilación; el ternario
  lo convierte en un dato mal mostrado que nadie ve hasta la demo.

---

### 4. Los KPI del panel mostraban una tendencia inventada

- **Paso:** `/admin/dashboard` → las cuatro tarjetas de arriba.
- **Error exacto:** cada una llevaba un sparkline rotulado `tendencia (ilustrativa)` sobre
  una serie fija en el código (`[4.2, 4.4, 4.5, 4.7, …]`) con el valor real pegado como
  último punto. Dos tarjetas ("Ingresos mensuales" y "Trans. recientes") compartían la
  misma serie, así que dibujaban la misma curva para métricas distintas.
- **Causa:** relleno visual de la maqueta que nunca se reemplazó por datos.
- **Arreglo:** fuera el sparkline. Cada tarjeta lleva un pie con un dato real derivado de
  lo que ya trae el endpoint: promedio por contrato (ingresos ÷ contratos vigentes) y
  fecha del último movimiento; las otras dos, un descriptor sin cifras.
- **Lección:** en una demo comercial, un gráfico rotulado "ilustrativo" no es neutral —
  es lo primero que mira el cliente y lo único que le confirma que el resto puede ser
  inventado. O el dato es real, o no va.
