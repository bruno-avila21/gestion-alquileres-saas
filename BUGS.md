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

---

### 5. Los botones "Estado ▾" e "Índice ▾" de Contratos no hacen nada

- **Paso:** `/admin/contratos` → clic en "Estado ▾" o en "Índice ▾".
- **Error exacto:** ninguno. No pasa nada: no abren menú, no filtran, no cambian de
  estado visual. Es la pantalla que más se muestra en una demo.
- **Reproducir:** abrir la pantalla y hacer clic. Los dos son `<button className="btn
  btn--sm">` sin `onClick` (`ContratosPage.tsx:417-418`).
- **Causa:** quedaron de la maqueta. El filtro por estado se implementó después, pero
  como solapas debajo de la barra —con el conteo al lado—, y nadie borró los botones
  viejos. El de índice nunca se implementó.
- **Arreglo:** "Estado ▾" se elimina: las solapas ya hacen ese trabajo y lo hacen mejor
  (muestran cuántos hay en cada uno). Dos controles para lo mismo confunden más de lo
  que ayudan. "Índice ▾" pasa a ser un `<select className="select select--inline">` real,
  con las opciones derivadas de `ADJ_LABELS` para que agregar un tipo de ajuste no deje
  el filtro atrasado. De paso: los conteos de las solapas se calculan sobre lo que pasó
  la búsqueda y el índice, para que filtrar por ICL no deje "Vigentes · 10" arriba de una
  tabla de 4; y el estado vacío distingue "no hay contratos" de "ninguno coincide".
- **Lección:** un control que no hace nada es peor que la ausencia del control. Si la
  maqueta trae botones sin conducta, o se cablean en el mismo commit o se borran.

---

### 6. Superficies cien veces más grandes en la cartera ("PH · 8.659 m²")

- **Paso:** `/admin/propiedades` o el sitio público → ficha de "Andrade al 1500".
- **Error exacto:** `8.659 m²` para un PH de 3 ambientes con 51 m² cubiertos. 16 de las
  85 propiedades importadas, entre ellas dos lotes de "85.071 m²" y "74.854 m²".
- **Reproducir:**
  ```
  python tools/sanear-superficies/sanear.py --api … --org palavecino --email … --password …
  ```
  (simulacro: lista las 16 sin escribir nada)
- **Causa:** `tools/importar-sitio-tokko/scrape.py`, función `numero()`. En la misma
  ficha de Tokko conviven **dos formatos de número**: el precio en es-AR, con el punto
  como separador de miles (`"$ 800.000"`), y las superficies con el punto como separador
  **decimal** (`'terreno': '86.59 m²'`, `'descubierta': '19.7 m²'`). `numero()` se
  escribió para el precio y borraba todos los puntos, así que `86.59` entraba como
  `8659`. En las 85 propiedades no hay un solo punto de superficie seguido de tres
  dígitos: el formato es inequívoco, sólo que nadie lo miró.
- **Arreglo:** `numero()` distingue por la forma del número, que es lo único que las dos
  convenciones no comparten: un grupo de miles **siempre** tiene tres dígitos, de modo
  que un punto final seguido de una o dos cifras sólo puede ser un decimal. Los datos ya
  cargados se corrigen con `tools/sanear-superficies/sanear.py`, que vuelve a parsear los
  atributos crudos guardados en `salida-palavecino/propiedades.json` en vez de adivinar
  dónde iba la coma a partir del número roto, y sólo pisa una propiedad si el valor que
  tiene hoy es exactamente el que producía el parseo viejo (si alguien la editó a mano,
  la deja y lo avisa). Idempotente.
- **Lección:** segunda vez que el mismo scraper mete basura en la base (ver #2) y las dos
  veces el error estaba a la vista en el JSON de origen. Antes de importar 85 fichas
  conviene mirar los extremos de cada campo numérico —máximo, mínimo, y los que
  contradicen a otro campo (total < cubierta)—; cuesta un minuto y no depende de que
  alguien note el absurdo en pantalla tres semanas después.

---

### 7. Producción caída tras el deploy: `column o.panel_palette does not exist`

- **Paso:** `git push` (Railway despliega solo) → entrar a `/admin/login`.
- **Error exacto:** el login devuelve `HTTP 500 {"error":"Internal server error"}`. En
  los logs de la API:
  ```
  SqlState: 42703
  MessageText: column o.panel_palette does not exist
  at ...Features.Auth.Commands.LoginCommandHandler.Handle(...):line 29
  ```
- **Reproducir:** desplegar cualquier commit que traiga una migración pendiente. El
  servicio arranca y `/health` responde 200 —el healthcheck no toca la base—, así que
  Railway marca el deploy **SUCCESS** con la aplicación inutilizable.
- **Causa:** la API no aplica migraciones al arrancar (no hay `Migrate()` en `Program.cs`)
  y el despliegue de Railway tampoco las corre. Las dos migraciones del rediseño
  (`AddOrganizationPanelPalette`, `AddSiteSettings`) estaban aplicadas sólo en local; el
  código nuevo consultaba una columna que en producción no existía.
- **Arreglo:** aplicarlas contra el proxy público de Postgres:
  ```
  cd api && dotnet ef database update \
    --project src/GestionAlquileres.Infrastructure \
    --startup-project src/GestionAlquileres.API \
    --connection "Host=<RAILWAY_TCP_PROXY_DOMAIN>;Port=<PORT>;Database=railway;Username=postgres;Password=…;SSL Mode=Require;Trust Server Certificate=true"
  ```
  (la cadena de la variable de entorno usa `postgres.railway.internal`, que no se resuelve
  desde afuera; el proxy sale de `railway variables --service Postgres`).
- **Lección:** el healthcheck que no toca la base convierte un deploy roto en un deploy
  "exitoso". Mientras las migraciones se apliquen a mano, **correrlas es parte del push,
  no un paso posterior** — y `/health` debería hacer una consulta real para que Railway
  se entere. Pendiente decidir entre migrar al arrancar o un release command.

---

### 8. El botón "Editar" de la ficha de contrato no hace nada, y seis campos usan una clase CSS inexistente

- **Paso:** `/admin/contratos/{id}` → clic en "Editar" (arriba a la derecha). Y en la misma
  pantalla, abrir "Registrar pago" o "Ajuste manual".
- **Error exacto:** ninguno. "Editar" no abre nada (`ContratoDetailPage.tsx:572`, un
  `<button>` sin `onClick`). Los seis campos de los dos formularios llevan
  `className="inp"`, clase que **no existe** en ningún CSS del proyecto: quedan con el
  estilo por defecto del navegador, desalineados con el resto del panel — y es la pantalla
  donde se carga la plata.
- **Reproducir:** `grep -c 'className="inp"' src/portal-admin/pages/ContratoDetailPage.tsx`
  → 6. `grep -n '\.inp[ ,{]' src/index.css` → vacío.
- **Causa:** las dos cosas son restos de la maqueta. El endpoint `PUT /contracts/{id}` y el
  hook `useUpdateContract` existían y estaban probados desde hacía meses; lo único que
  faltaba era la pantalla, así que para corregir el alquiler, la indexación o el punitorio
  de un contrato ya cargado había que ir por la API. Se nota apenas se pactan punitorios
  sobre una cartera existente.
- **Arreglo:** `inp` → `input`. Y el botón abre `EditarContratoModal`, que reusa los campos
  del alta en vez de copiarlos: se extrajeron a `ContratoFormFields` + el modelo en
  `utils/contractForm.ts` (el lint exige que un archivo con componentes exporte sólo
  componentes). Dos detalles que aparecieron al escribirlo:
  - **Un contrato rescindido no se puede editar** —el handler lo rechaza—, así que el botón
    se deshabilita en vez de ofrecer una acción que iba a fallar.
  - **Los selects ofrecen la propiedad y el inquilino actuales aunque estén dados de baja.**
    El alta filtra por `isActive`; heredar ese filtro habría dejado el select vacío al
    editar un contrato cuya propiedad se desactivó, y guardar lo habría reasignado a otra
    sin que se note.
  Verificado contra producción: `PUT /contracts/{id}` con el cuerpo que arma
  `contractFormToRequest` devuelve 200 y, reenviando los valores actuales, no cambia nada.
- **Lección:** tercera vez en dos días que un control de la maqueta llegó a producción sin
  conducta (ver #5). El patrón es siempre el mismo: la pantalla se arma con el diseño
  completo y el cableado llega por partes. Vale la pena que un revisor busque
  `<button` sin `onClick` y `className` que no resuelva a ninguna regla CSS antes de dar
  una pantalla por terminada.

---

### 9. El sitio público tapaba la foto del hero y pintaba de oscuro dos bloques del modo claro

- **Paso:** `/sitio/palavecino` en modo claro. Y dentro de una propiedad, el panel de precio
  de la derecha ("Venta / US$130.000").
- **Error exacto:** ninguno en consola; es visual. El hero se veía como un fondo negro: la
  foto llegaba con `brightness(.42)` y encima dos degradados al 58% y al 82% de opacidad.
  Multiplicado, por el lado del título pasaba **menos del 5%** de la imagen. En la ficha, la
  cabecera de precio usaba `background: var(--slate)` (pizarra fija), así que con la página
  en claro ese bloque parecía haber quedado en modo oscuro.
- **Reproducir:** `agent-browser get styles ".hero-bg img"` → `filter: brightness(0.42)…`;
  `agent-browser get styles ".cc-head"` → `background-color: rgb(33, 49, 69)` con el sitio
  en claro.
- **Causa:** el velo se calibró para que el título blanco pasara contraste sin depender de
  la foto, y se fue de mano al sumar dos degradados que se multiplican. La pizarra de
  `.cc-head` venía del modelo de diseño, donde ese bloque es una "cinta invertida" — pero en
  el modelo el precio va en el acento **sobre superficie clara**, no al revés.
- **Arreglo:** `brightness(.72)` y degradados bajados a .52/.9 en los extremos; el título
  compensa con `text-shadow` en vez de con el velo. `.cc-head` pasa a `--violet-tint` con
  tinta `--ink`, que siguen al tema.
- **Lección:** un scrim de dos degradados hay que mirarlo multiplicado, no sumado. Y
  cualquier bloque con color fijo (`--slate`, `#fff`, `#000`) es un candidato a verse "del
  otro tema": si el sitio tiene claro y oscuro, el color va en variable o no va.

---

### 10. El hover borraba la opción elegida en los segmentos (Todas / Comprar / Alquilar, y Operación)

- **Paso:** `/sitio/palavecino` → pasar el mouse por encima de la solapa ya seleccionada.
  Igual en `/propiedades` → filtro "Operación" (Todas / Venta / Alquiler / Temporario) y
  "Precio" (Todas / USD / ARS).
- **Error exacto:** `.seg button:hover { color: var(--ink) }` no excluía `.on`, así que la
  opción seleccionada perdía el violeta al pasarle el mouse y se leía como deseleccionada.
  Además, en modo oscuro lo seleccionado quedaba en violeta aclarado sobre superficie
  oscura: se confundía con el resto.
- **Reproducir:** `agent-browser hover ".seg button.on"` y leer `color` computado: daba
  `rgb(15, 23, 42)` en vez del acento.
- **Causa raíz (la que importa):** el acento elegido por la inmobiliaria se inyecta **en
  línea** en `.pp-app` (`accentVars`), y un estilo en línea le gana a cualquier regla de la
  hoja. El bloque `[data-theme="dark"]` de `publico.css` no podía redefinir `--violet-tint`,
  `--violet-edge` ni `--on-violet`: el modo oscuro se quedaba con los tintes casi blancos
  del claro. Se vio recién al pasar `.cc-head` a `--violet-tint` (texto blanco sobre lila).
- **Arreglo:** `:hover:not(.on)`, y `.on` con tinta `--ink` en oscuro. `accentVars` ahora
  recibe el tema y devuelve derivaciones oscuras (acento aclarado, tintes mezclados contra
  la superficie oscura, no contra negro).
- **Lección:** una variable de tema que se inyecta en línea deja de ser tematizable. Si el
  valor lo elige el usuario **y** depende del tema, el tema tiene que entrar en el cálculo —
  la hoja de estilos ya no llega a corregirlo.

---

### 11. El logo que se sube en Configuración → Marca no aparecía en el sitio público

- **Paso:** panel → Configuración → Marca → "Cambiar logo" → abrir `/sitio/{slug}`.
- **Error exacto:** ninguno; el sitio seguía mostrando el monograma con la inicial. Peor: la
  pantalla `/admin/sitio` **afirmaba** que "el logo … del pie del sitio sale de la marca de
  la inmobiliaria", que era falso.
- **Reproducir:** `curl .../api/v1/public/palavecino` → el DTO no traía ningún campo de logo.
- **Causa:** el logo se servía sólo por `GET /organization/logo`, que exige token del panel.
  El sitio público es anónimo, así que no tenía forma de pedirlo; se implementó para el
  encabezado de los PDF y nunca se extendió al sitio.
- **Arreglo:** `GET /public/{slug}/logo` (anónimo, cacheado un día, sólo organización
  activa) y `hasLogo` en `PublicOrganizationDto`. El encabezado, el pie y el panel
  institucional de la portada muestran el logo, y caen al monograma si no hay.
- **Lección:** cuando una pantalla del panel describe un efecto ("esto se ve en tu sitio"),
  esa frase es un contrato. Si el efecto no existe, el texto miente y nadie lo nota hasta
  que el cliente pregunta.

---

### 12. "No pude entrar a la pantalla de Admin" — NO REPRODUCE

- **Lo que dijo Bruno (2026-09-10):** *"no pude entrar a la pantalla de Admin, me dice
  'Revisá la organización, el email y la contraseña: alguno no coincide.'"*
- **Estado: abierto, sin reproducir.** La credencial de la demo funciona hoy, verificada por
  los dos caminos:
  - API: `POST /api/v1/auth/login` con `{palavecino, admin@palavecino.demo, Palavecino2026!}`
    → **200** con token. También 200 con el email en mayúsculas y con un `Authorization`
    vencido pegado en el encabezado (dos hipótesis descartadas).
  - Navegador: `agent-browser` completó el formulario de `/admin/login` en producción y
    terminó en `/admin/dashboard`.
- **Lo que el código no permite distinguir:** el 401 es opaco a propósito (auditoría M2,
  `LoginCommandHandler`): organización inexistente, email inexistente, contraseña mala y
  organización suspendida devuelven todos lo mismo, con un `BCrypt.Verify` contra un hash
  señuelo para igualar los tiempos. **No se toca**: el mensaje genérico es la defensa contra
  enumeración de inmobiliarias y usuarios.
- **Hipótesis que quedan, en orden:** (1) el navegador autocompletó email/contraseña de otra
  entrada guardada — el campo Organización viene precargado, así que el formulario se ve
  lleno y se manda con Enter; (2) se tipeó otra cosa en Organización (va el slug
  `palavecino`, no el nombre); (3) se probó con la credencial del portal de inquilinos.
- **Próximo paso:** que Bruno reporte los tres valores exactos que tipeó, o que mire la
  pestaña Red (el cuerpo del `POST /auth/login` dice qué se mandó).

---

### 13. Los segmentos no se leían como un control, y el 401 del login no decía qué campo fallaba

- **Lo que dijo Bruno (2026-09-10):** *"los botones de Todas Comprar Alquilar … no están bien
  para mí"*, con dos referencias: el `Pivot` de Fluent UI para las solapas de la portada y el
  `SegmentGroup` de Chakra (con `SegmentGroup.Indicator`) para el filtro Operación. Y:
  *"sigo sin poder ingresar … con el usuario que me pusiste en el artefacto"*.
- **Lo del login: la credencial de la guía es correcta y funciona.** `guia-demo.html:146-153`
  trae los tres datos bien (`palavecino` / `admin@palavecino.demo` / `Palavecino2026!`) y el
  `POST /auth/login` devuelve 200 con ellos. El problema es que el 401 nombra los tres campos
  sin decir cuál falló, así que no hay forma de avanzar: se prueba a ciegas.
- **Arreglo del login (no toca el backend):** el formulario, ante un 401, consulta
  `GET /public/{slug}` —anónimo, el mismo que dibuja el sitio de cada inmobiliaria— y
  distingue "esa organización no existe" de "la organización existe: es el email o la
  contraseña". **No debilita la defensa contra enumeración**: ese endpoint ya responde eso a
  cualquiera, es su función. Se suma un botón "Mostrar" en la contraseña, que es la
  hipótesis principal: el campo Organización viene precargado, así que el formulario se ve
  completo y el navegador puede autocompletar una credencial guardada sin que se note.
- **Arreglo de los segmentos:** un componente `SegmentGroup` con indicador deslizante y dos
  pieles — `underline` (Pivot) para la portada, `pill` (Chakra) para los filtros. El
  indicador se posiciona **midiendo el botón activo**, no con `ancho / cantidad`: los
  rótulos tienen largos distintos, el riel los acomoda en dos filas, y la inmobiliaria puede
  cambiar la tipografía del sitio desde el panel. Un `ResizeObserver` sobre la pista y sobre
  cada botón lo recalcula cuando la fuente de Google llega después del primer pintado.
- **Lección:** el `.on` anterior sólo cambiaba color de fondo y texto — no había nada que
  dijera "esto se mueve entre opciones". El indicador que viaja es lo que convierte tres
  botones sueltos en un control. Y en un mensaje de error, "revisá estos tres campos" es
  sólo un poco mejor que "error": si hay una forma legítima de saber cuál falló, usala.
