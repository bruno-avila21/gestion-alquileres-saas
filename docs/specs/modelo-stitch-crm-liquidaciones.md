# Especificación funcional y visual — Maquetas Stitch (CRM Leads + Liquidaciones)

Fuente: `Modelo-Stitch-Google/stitch_plataforma_crm_inmobiliaria_multi_tenant/`
Pantallas cubiertas:
1. `crm_leads_pipeline_web_desktop_1` (variante A del pipeline)
2. `crm_leads_pipeline_web_desktop_2` (variante B del pipeline, más pulida)
3. `liquidaciones_propietarios_calculadora_icl_ipc_web_desktop_1` (variante A)
4. `liquidaciones_propietarios_calculadora_icl_ipc_web_desktop_2` (variante B, más pulida)

Los pares (1/2) y (3/4) son la MISMA pantalla en dos iteraciones de diseño casi idénticas
en contenido y estructura, con diferencias menores de layout/espaciado y algún control
agregado. Documento cada par junto, marcando las diferencias.

Cliente demo: "Palavecino & Asoc." — inmobiliaria con sede en Delia 1077, San Miguel.
Ambas pantallas comparten el mismo shell de aplicación (sidebar + header), descripto una
sola vez al principio.

---

## 0. Shell común a las 4 pantallas

### 0.1 Sidebar izquierdo (fijo, ancho ~288px, alto completo)

De arriba abajo:
- **Marca**: ícono cuadrado con la "P" de Palavecino + nombre "Palavecino & Asoc." +
  subtítulo "Gestión Inmobiliaria v2.4".
- **Botón CTA destacado**: "Nueva Operación" (ícono `+`), ancho completo, color primario.
- **Navegación primaria** (lista de ítems con ícono + label, uno resaltado como activo
  según la pantalla):
  1. Dashboard
  2. Cartera & Propiedades
  3. Contratos & Alquileres
  4. Ajustes ICL / IPC
  5. Liquidaciones Propietarios
  6. CRM Leads — lleva un badge numérico "8 Nuevos" cuando no es la pantalla activa
  7. Configuración Portal
- **Bloque de estado inferior**: caja "Tokko Broker API" con punto verde animado
  (pulso), texto "En Línea" / "Sincronizado" y línea secundaria "Último sync: hace 4 min
  (1.248 fichas activas)".
- **Links de pie**: "Centro de Ayuda", "Soporte Tokko".

### 0.2 Header superior (sticky, alto 64px)

- **Izquierda**: indicador de sede activa ("Sede Central Delia 1077", con selector
  desplegable) + buscador global (placeholder cambia por pantalla: en CRM es "Buscar por
  cliente, DNI, o dirección…", en Liquidaciones es "Buscar por inquilino, propietario,
  CBU o contrato…").
- **Derecha**: chip informativo "BCRA ICL: +4.2% Hoy" (índice del día, siempre visible),
  botón de notificaciones (con punto rojo de "hay novedades"), botón de ayuda, y en CRM
  además un botón primario "Nuevo Lead"; separador vertical; avatar + nombre del usuario
  logueado ("Martín Gómez") y su rol ("Broker Matr. 7482" en CRM, "Jefe de
  Liquidaciones" en Liquidaciones).

---

## 1. Pantalla: CRM — Pipeline de Leads (Kanban)

Variantes: `crm_leads_pipeline_web_desktop_1` y `_2`. La versión 2 agrega una franja de
"Canales Activos" bajo el título, un botón secundario "Ficha" en las tarjetas de la
columna 1, y separa mejor los espaciados; el contenido y la lógica son iguales.

### 1.1 Estructura de la página (de arriba abajo)

1. Header de sección: breadcrumb "GESTIÓN COMERCIAL › TOKKO & PORTALES" + título
   "Pipeline & Gestión de Leads Inmobiliarios".
2. Fila de botones utilitarios (alineados a la derecha del título).
3. (Solo variante 2) Franja "Canales Activos" con indicador en vivo + chips de canal +
   "Última sincronización automática: hace 4 minutos".
4. Grilla de 4 tarjetas KPI.
5. Barra de filtros y selector de vista.
6. Tablero Kanban de 5 columnas con scroll horizontal (cada columna hace scroll vertical
   independiente).

### 1.2 Controles interactivos

**Botones utilitarios de la cabecera de sección** (estilo secundario, con ícono):
- "Sincronizar Tokko API" (variante 2: "Sincronizar Tokko") — ícono sync.
- "Importar CSV" — ícono file_upload.
- "Exportar Informe" — ícono download.

**Selector de vista** (segmented control, 3 opciones, "Kanban" activa por defecto):
- "Kanban" (ícono view_kanban)
- "Lista" (ícono table_rows)
- "Calendario" (ícono calendar_today)

**Filtros rápidos** (dropdowns tipo chip, muestran valor actual + flecha):
- "Origen: Todos los Portales" (ícono hub)
- "Asignado: Todo el Staff" (ícono account_circle)
- "Venta & Alquiler" (ícono sell, filtro de tipo de operación)
- Campo de texto libre "Filtrar por interesado o propiedad…" (ícono filter_alt)

En la variante 2 el filtro de canal se reemplaza visualmente por la franja de "Canales
Activos" (chips: WhatsApp Directo, Zonaprop, Web Palavecino, Argenprop) — no queda claro
si son clicables como filtro o solo informativos; tratarlos como toggles de filtro por
canal es la interpretación funcional razonable.

### 1.3 KPIs de la cinta superior (4 tarjetas, mismo contenido en ambas variantes)

| KPI | Valor | Dato secundario |
|---|---|---|
| Total Leads Activos | 148 | "+12 esta semana" (chip verde) |
| Leads Calificados | 42 | "Alta Intención" (chip primario) |
| Visitas Agendadas (Semana) | 19 | "6 hoy" (chip verde) |
| Tasa de Conversión | 14.2% | "+1.8% vs Feb" (chip verde) |

### 1.4 El pipeline (tablero Kanban) — 5 columnas/etapas

Cada columna tiene: punto de color + nombre + contador de leads en badge; algunas
muestran además un valor potencial en dólares o un menú "⋮".

1. **Nuevos Contactos** (8) — "USD 1.8M pot." en la cabecera.
2. **Contactados & Calificando** (14)
3. **Visita Coordinada** (9) — se llama "Visita Coordinada / En Proceso" en variante 1.
4. **Propuesta / Reserva** (5) — variante 2 la llama "Propuesta / Negociación".
5. **Cierre & Boleto / Firma** (3)

Las columnas son de ancho fijo (~300–320px) dentro de un contenedor con scroll
horizontal; cada tarjeta es arrastrable (cursor "grab") — sugiere drag & drop entre
columnas como interacción principal del pipeline.

### 1.5 Anatomía de la tarjeta de lead (varía el contenido según la etapa)

Elementos comunes a toda tarjeta:
- Badge de origen/canal arriba a la izquierda (ícono + texto, color por canal: WhatsApp
  verde, Zonaprop azul, Web Palavecino violeta) o badge de urgencia ("Urgencia Alta"
  ámbar, "Urgencia Media" neutro) según la columna.
- Marca de tiempo relativa arriba a la derecha ("Hace 12 min", "Ayer", "Hoy 11:20").
- Nombre del lead/cliente en negrita, tamaño destacado.
- Línea de descripción/contexto (1-2 líneas, truncada) con el interés o consulta.
- Chips de datos (rango de precio, tipo de propiedad, "Permuta Posible", etc.).
- Pie de tarjeta con separador: identificador Tokko (#TK-xxxx), avatar circular con
  iniciales del agente asignado + nombre, y una acción o dato final (botón, monto, o
  estado).

Variaciones por columna:
- **Columna 1 (Nuevos Contactos)**: pie con botón de acción "Contactar" (verde, ícono
  send) o "Llamar" (ícono call). Variante 2 agrega un segundo botón "Ficha" junto al
  principal.
- **Columna 2 (Contactados)**: caja de "Nota:" con comentario libre del agente; pie con
  avatar + nombre del agente y el monto de interés del lead (ej. "USD 320.000").
- **Columna 3 (Visita Coordinada)**: banner superior con fecha/hora de la visita
  ("Mañana 15:30 hs"), dirección de la propiedad con ícono de pin, estado de
  confirmación por WhatsApp ("Confirmado" / "Recordatorio hoy"), dato de llave/acceso;
  pie con agente asignado + botón de navegación (ícono navigation).
- **Columna 4 (Propuesta/Reserva)**: badge de tipo de operación ("Reserva
  Ad-Referéndum", "Alquiler Comercial") + contador de días restantes en rojo si aplica;
  caja financiera con precio publicado (tachado), oferta formal destacada y seña
  recibida "(en custodia)"; pie con estado ("Contraoferta en análisis") y botón "Ver
  Dictamen".
- **Columna 5 (Cierre & Firma)**: badge de hito legal ("Boleto de Compraventa", "Firma
  Contrato Locación") + fecha; caja con monto de operación, comisión proyectada (%) y
  escribanía interviniente; pie con estado ("Certificados Listos") y botón/ícono de
  carpeta o estado "Para Firma".

### 1.6 Paneles laterales / drawers / modales

No hay drawer, modal ni vista de detalle de lead en ninguna de las dos maquetas — todo
el flujo vive dentro de las tarjetas del Kanban. Una vista de detalle de lead (al hacer
click en la tarjeta) queda como gap de diseño a resolver en la implementación.

### 1.7 Intención de layout

- Sidebar fijo de ancho fijo, contenido principal con margen izquierdo equivalente y
  scroll propio.
- Grilla de KPIs: 1 columna en mobile, 2 en tablet, 4 en desktop.
- El tablero Kanban fuerza un ancho mínimo total (~1450-1550px) para que las 5 columnas
  no se compriman — en pantallas más chicas aparece scroll horizontal.
- Cada columna del Kanban es una tarjeta contenedora con cabecera fija y cuerpo con
  scroll vertical independiente (sticky header por columna).

---

## 2. Pantalla: Liquidaciones a Propietarios & Calculadora ICL/IPC

Variantes: `liquidaciones_propietarios_calculadora_icl_ipc_web_desktop_1` y `_2`. Mismo
contenido de negocio; la variante 2 reordena la cabecera (título arriba, botones debajo
en su propia fila, franja de período horizontal debajo de los botones), cambia el label
del botón masivo y agrega un dato de "Cierre de lote". Documento el contenido unificado
y marco las diferencias donde importan.

### 2.1 Estructura de la página (de arriba abajo)

1. Header de sección: título "Liquidación Mensual a Propietarios & Ajustes ICL / IPC" +
   chip "Período Activo".
2. Fila de 3 botones de acción principales.
3. Franja de información de período: "Período: Octubre 2024 • Base de cálculo según
   índice BCRA (Ley de Alquileres 27.551 / DNU 70/2023)" (variante 2 le agrega, al
   costado, "Cierre de lote: 15/10/2024").
4. Grilla de 4 tarjetas KPI.
5. Layout de 2 columnas (aprox. 60% / 40%):
   - **Columna izquierda**: tabla de contratos/liquidaciones + tarjeta explicativa de la
     calculadora oficial BCRA (ICL/IPC).
   - **Columna derecha** (sticky, se mantiene visible al hacer scroll): preview del
     recibo/comprobante de liquidación en formato PDF, con sus acciones.

### 2.2 Controles interactivos — los 3 botones pedidos explícitamente

Los tres viven en la misma fila, a la derecha del título (variante 1) o debajo del
título (variante 2). Textos EXACTOS:

1. **"Simular Ajuste ICL/IPC"** — botón secundario (fondo claro, borde), ícono
   `calculate` en color primario. Abre (funcionalmente) una simulación de cálculo de
   ajuste sin persistir nada — no hay modal en la maqueta, pero por el ícono de
   calculadora y el texto "Simular" se infiere una acción que NO dispara el flujo real
   de ajuste, solo previsualiza el resultado.
2. **"Exportar a Banco (CBU/Alias)"** — botón secundario, ícono `file_download` en color
   secundario. Dispara la generación de un archivo/lote para transferencias bancarias
   (formato de banco, usando CBU/Alias de cada propietario visible en la tabla).
3. **"Liquidación Masiva por Lote"** (variante 1) / **"Nueva Liquidación Masiva"**
   (variante 2) — botón PRIMARIO (fondo sólido color primario, el más destacado de los
   tres), ícono `payments`. Dispara el proceso de generar/cerrar la liquidación de todo
   el lote de contratos del período. El nombre correcto a usar en la implementación es
   el de la variante 2, **"Nueva Liquidación Masiva"**, por ser el más reciente y el que
   nombró el usuario.

### 2.3 KPIs de la cinta superior (4 tarjetas)

Contenido idéntico entre variantes salvo el KPI 4, que cambia de encuadre:

| KPI | Valor | Dato secundario |
|---|---|---|
| Total Cobrado Inquilinos | $ 18.450.000 ARS | "+96% cobrado en fecha (estipulada)" |
| Liquidado a Propietarios | $ 16.236.000 ARS | "88% del total general procesado" |
| Comisión Inmobiliaria Total / Comisión Honorarios | $ 1.476.000 ARS | chip "8% Honorarios" + "administración regular" |
| Pendientes de Transferencia (v1) / Contratos al Día (v2) | "4 contratos" (v1) / "38 de 42" (v2) | v1: "Requieren validación o clearing de CBU"; v2: "4 pendientes de clearing CBU" |

Nota: el KPI 4 en variante 1 enfatiza lo pendiente; en variante 2 enfatiza lo que ya está
al día (mismo dato de fondo: 4 contratos con problema de CBU sobre 42 totales).

### 2.4 Tabla — "Detalle de Contratos & Liquidaciones"

**Cabecera de la tabla**: ícono + título + contador "3 de 42 contratos" + tabs de filtro
rápido: **"Todos"** (activo), **"Listos"**, **"Transferidos"**.

**Columnas exactas** (7):
1. Propiedad / Inquilino
2. Propietario & CBU
3. Alquiler / Ajuste
4. Retenciones
5. Neto Liquidado
6. Estado
7. Acciones (alineada a la derecha)

**Qué lleva cada celda:**
- *Propiedad / Inquilino*: nombre/dirección de la propiedad en negrita, nombre del
  inquilino (con ícono de persona), y un chip pequeño con el marco legal del contrato
  ("Contrato Ley 27.551", "DNU 70/2023 Trimestral", "Comercial IPC Semestral").
- *Propietario & CBU*: nombre del propietario, CBU en fuente monoespaciada, y banco +
  tipo de cuenta ("Banco Galicia • C/C").
- *Alquiler / Ajuste*: monto del alquiler y, debajo, un chip con el tipo/porcentaje de
  ajuste aplicado ("ICL +142.8%", "IPC Trim. +12.4%") o "Sin Ajuste este mes" si no
  corresponde ajuste ese período.
- *Retenciones*: monto negativo en rojo (fuente monoespaciada) + descripción ("8%
  Honorarios").
- *Neto Liquidado*: monto final destacado + label "Neto ARS".
- *Estado*: chip/pill con color semántico:
  - Ámbar con punto pulsante: **"Listo para Transferir"**.
  - Verde con check: **"Transferido"** (+ referencia de comprobante, ej. "Comp. #TR-8841").
  - Ámbar claro con reloj de arena: **"Pendiente de Cobro"** (el inquilino todavía no
    pagó, por eso no se puede liquidar).
- *Acciones*: 2-3 botones ícono según el estado de la fila:
  - "Ver Recibo Oficial" (ícono receipt) — siempre presente.
  - Si está listo para transferir: "Transferir Ahora" (ícono send_money, botón
    primario) + "Enviar WhatsApp" (ícono chat).
  - Si ya fue transferido: "Descargar Comprobante Bancario" (ícono attachment) +
    "Enviar WhatsApp".
  - Si está pendiente de cobro: "Recordar Pago Inquilino" (ícono mail) + "Contactar"
    (ícono chat).

**Filas de ejemplo (datos demo, 3 de 42):**
1. Depto 3 Amb Quirno 800 — Facundo Soria (inquilino) / Ing. Roberto Arévalo
   (propietario) — $450.000, ICL +142.8% — retención -$36.000 — neto $414.000 — Listo
   para Transferir. (Fila resaltada/seleccionada, sirve de fuente para el preview del
   recibo a la derecha).
2. Casa Bella Vista Av. Francia 1400 — Estudio Jurídico Díaz & Cía / Lic. Marta P.
   Gómez — $820.000, IPC Trim. +12.4% — retención -$65.600 — neto $754.400 —
   Transferido (Comp. #TR-8841).
3. Local Comercial Delia 1100 — Farmacia & Perfumería San Miguel / Fideicomiso San
   Miguel — $1.200.000, sin ajuste este mes — retención -$96.000 — neto $1.104.000 —
   Pendiente de Cobro.

**Pie de tabla**: texto "Mostrando liquidaciones del lote central #LQ-2024-10-A" + link
"Ver Historial de Liquidaciones Anteriores →".

### 2.5 Tarjeta "Calculadora Oficial BCRA (ICL / IPC)" (debajo de la tabla, misma columna)

- Encabezado con ícono + título + badge verde "Datos Oficiales en Vivo".
- Texto explicativo: los coeficientes se sincronizan automáticamente con BCRA/INDEC; los
  contratos bajo Ley 27.551 usan la variación acumulada anual del ICL.
- 3 mini-tarjetas de datos en fila:
  1. "ICL Últimos 12 Meses" → +142.8% (coeficiente 2.428).
  2. "IPC Trimestral Acumulado" → +12.4% (Jul-Sep 2024).
  3. "IPC Mensual INDEC" → +3.5% (publicación oficial mensual).

Esta tarjeta es puramente informativa/de referencia — no tiene inputs; funciona como
contexto para el botón "Simular Ajuste ICL/IPC".

### 2.6 Panel lateral derecho — Preview del recibo de liquidación (sticky)

Actúa como vista de detalle/documento del contrato seleccionado en la tabla (fila 1 en
el ejemplo). Estructura de arriba a abajo:

1. (Solo variante 1) Barra de control superior duplicando los botones de acción.
2. **Simulación de hoja PDF** (fondo blanco, dentro de la tarjeta con fondo gris):
   - Membrete institucional: logo/inicial + "Palavecino & Asoc." + matrícula CUCICBA/
     CMCPSI + dirección de sede + email de administración; a la derecha, badge
     "LIQUIDACIÓN MENSUAL" (v1) / "RECIBO OFICIAL DE LIQUIDACIÓN" (v2) + fecha de
     emisión + período.
   - Caja de datos del propietario/beneficiario: nombre, badge "Contrato Vigente", CUIT,
     inmueble, inquilino, y línea con CBU + alias del banco.
   - **Desglose de liquidación** (tabla simple de líneas, cada una label + monto en
     fuente monoespaciada):
     - Alquiler Básico Contrato → $185.337,72
     - Ajuste Oficial BCRA (Ley 27.551) — "Coeficiente ICL Anual (+142.8%)" → +$264.662,28
     - Subtotal Cobrado a Inquilino (fila destacada en negrita) → $450.000,00
     - Honorarios de Administración Inmobiliaria — "8% sobre canon locativo" (en rojo) →
       -$36.000,00
     - Retención Impuesto de Sellos / IIBB (CABA/PBA) — "Exento según régimen locatario
       único" → $0,00
     - Expensas Extraordinarias / Gastos Mantenimiento — "Sin débitos adicionales en
       período" → $0,00
   - **Total Neto a Acreditar**: caja destacada en color primario sólido, monto grande
     $414.000,00, aclaración "Acreditación vía Interbanking / DEBIN" y "PESOS
     ARGENTINOS".
   - Sello legal / disclaimer en texto chico: documento emitido electrónicamente,
     conforme normativas AFIP y CUCICBA; el comprobante bancario se adjunta tras la
     transferencia.
3. **Acciones del documento** (fila de botones, al pie del panel):
   - **"Enviar por WhatsApp con 1 Click al Propietario"** — botón primario ancho
     completo, verde (WhatsApp), ícono chat. Es la acción más destacada del panel.
   - **"Descargar PDF"** — secundario, ícono download.
   - **"Imprimir Recibo"** — secundario, ícono print.
   (En variante 1 estos 3 botones aparecen duplicados también arriba del documento, a
   modo de barra de control; en variante 2 solo aparecen una vez, al pie — usar el
   patrón de variante 2 para la implementación: barra de acciones única al pie del
   panel.)

### 2.7 Intención de layout

- Layout de 12 columnas: tabla + calculadora ocupan 7/12 (~58%), el panel de recibo
  ocupa 5/12 (~42%).
- El panel del recibo es **sticky** (se mantiene fijo en pantalla) mientras el usuario
  scrollea la tabla de la izquierda — pensado para revisar/enviar el recibo mientras se
  navega la lista de contratos.
- La tabla tiene scroll horizontal propio si el contenido no entra (columnas con
  anchos mínimos definidos en variante 2, ej. min 190px, 180px, 130px, etc., para evitar
  que los datos se compriman ilegibles).
- KPIs en grilla de 4 columnas en desktop, colapsando a 2/1 en pantallas chicas.

---

## 3. Notas para la implementación en React

- No hay ningún modal/drawer real en ninguna de las 4 maquetas: "Simular Ajuste
  ICL/IPC" necesita definirse desde cero (probablemente un modal o panel con inputs de
  fecha/índice/monto base, ya que la maqueta solo da el botón disparador y la tarjeta de
  contexto de la calculadora).
- El botón masivo se debe implementar con el label **"Nueva Liquidación Masiva"**
  (variante 2, la más reciente).
- Los estados de la tabla de liquidaciones ("Listo para Transferir", "Transferido",
  "Pendiente de Cobro") determinan qué acciones-ícono se muestran en la columna
  Acciones — conviene modelarlo como un mapa estado → set de acciones, no como
  condicionales sueltos.
- El pipeline de leads no tiene vista de detalle: si el negocio la necesita, es una
  decisión de producto no cubierta por el diseño, no un olvido de esta lectura.
- Los montos en USD (pipeline) y ARS (liquidaciones) conviven; respetar el formato de
  miles con punto y decimales con coma (es-AR) tal como aparece en las maquetas
  ($185.337,72).
