# "Diseño Libre" en stock-product — investigación y portabilidad a gestion-alquileres-saas

Investigación de código real (no documentación aspiracional) del repo
`D:\Proyectos_Propios\Trabajo\stock-product\stock-product`, un SaaS multi-tenant de
e-commerce (tipo Tiendanube). Objetivo: decidir si el sistema de personalización visual
("Diseño Libre") es reutilizable para `gestion-alquileres-saas` (.NET 8 + React 19 + TS + Vite
+ PostgreSQL/EF Core, multi-tenant vía `OrganizationId`).

## 0. Contexto general de stock-product

- **Backend:** Node.js/Express, **una base de datos PostgreSQL POR TENANT** (no discriminador
  `OrganizationId` en filas compartidas). El runner de migraciones (`backend/scripts/migrate.js`)
  corre cada archivo de `backend/migrations/*.sql` contra cada tenant DB. Esto es una diferencia
  arquitectónica de fondo frente a gestion-alquileres-saas (DB compartida + filtro global EF Core).
- **Tres apps front separadas y sin paquete compartido:** `frontend/` (panel admin del comerciante),
  `storefront/` (tienda pública, Astro + React, con SSR), `mobile-cap/` (versión Capacitor/APK).
  Ciertos archivos de configuración están literalmente **duplicados a mano** entre las tres
  (ver `storefront/src/config/templatesEditables.js`, comentario: "Archivo DUPLICADO idéntico en
  `storefront/src/config/` y `mobile-cap/src/config/`... copiar verbatim al tocarlo").
- **Stack de theming real:** React 18.3 (no 19), JavaScript plano con JSX (`.jsx`, cero `.tsx`,
  cero TypeScript en frontend/storefront), Tailwind 3.4 solo como utilidades de layout, **sin
  ninguna librería de theming** (no styled-components, no zustand, no CSS-in-JS). Todo el
  mecanismo de personalización es **CSS custom properties nativas + `document.documentElement`
  + React Context**, cero dependencias externas.

## 1. Dónde vive la configuración de tema/marca

Dos piezas de storage, con roles distintos:

### 1.1 Tabla `tenant_themes` (colores fijos por columna)
`backend/migrations/003_theme_system.sql`:
```sql
CREATE TABLE IF NOT EXISTS tenant_themes (
  id               SERIAL PRIMARY KEY,
  tenant_id        UUID NOT NULL UNIQUE,
  primary_color    VARCHAR(7)   NOT NULL DEFAULT '#3b82f6',
  primary_hover    VARCHAR(7)   NOT NULL DEFAULT '#2563eb',
  secondary_color  VARCHAR(7)   NOT NULL DEFAULT '#8b5cf6',
  success_color    VARCHAR(7)   NOT NULL DEFAULT '#10b981',
  danger_color     VARCHAR(7)   NOT NULL DEFAULT '#ef4444',
  warning_color    VARCHAR(7)   NOT NULL DEFAULT '#f59e0b',
  bg_base          VARCHAR(7)   NOT NULL DEFAULT '#f9fafb',
  bg_card          VARCHAR(7)   NOT NULL DEFAULT '#ffffff',
  bg_sidebar       VARCHAR(7)   NOT NULL DEFAULT '#111827',
  text_primary     VARCHAR(7)   NOT NULL DEFAULT '#111827',
  text_secondary   VARCHAR(7)   NOT NULL DEFAULT '#6b7280',
  border_color     VARCHAR(7)   NOT NULL DEFAULT '#e5e7eb',
  font_heading     VARCHAR(100) NOT NULL DEFAULT 'Inter',
  font_body        VARCHAR(100) NOT NULL DEFAULT 'Inter',
  spacing_unit     INTEGER      NOT NULL DEFAULT 8,
  radius_default   INTEGER      NOT NULL DEFAULT 8,
  brand_animation_enabled BOOLEAN NOT NULL DEFAULT true,
  dark_mode_enabled       BOOLEAN NOT NULL DEFAULT false,
  template_name    VARCHAR(100),
  ...
);

CREATE TABLE IF NOT EXISTS theme_presets (
  id SERIAL PRIMARY KEY, industry VARCHAR(50), name VARCHAR(100),
  config JSONB NOT NULL, description TEXT, ...
);
```
Es el tema del **panel admin** (dashboard del comerciante), un esquema rígido columna-por-columna.

### 1.2 `tienda_config` (marca + secciones del storefront público)
`backend/migrations/001_schema_inicial.sql` (L136-153):
```sql
CREATE TABLE IF NOT EXISTS tienda_config (
    id          INTEGER PRIMARY KEY DEFAULT 1,
    branding    TEXT NOT NULL DEFAULT '{}',   -- nombre, tagline, logoUrl, whatsapp, email...
    navigation  TEXT NOT NULL DEFAULT '[]',
    copywriting TEXT NOT NULL DEFAULT '{}',   -- hero_title, footer_copyright, etc.
    theme       TEXT NOT NULL DEFAULT '{}',
    updated_at  TIMESTAMP DEFAULT NOW()
);
```
Ampliada más tarde por migraciones incrementales:
- `006_template_plan.sql`: `ADD COLUMN template_id TEXT NOT NULL DEFAULT 'automotriz-clasico'`
  (qué plantilla/layout usa el storefront de este tenant).
- `026_template_config.sql`: `ADD COLUMN template_config JSONB DEFAULT '{}'` — **acá vive
  el builder de "Diseño Libre"**: `template_config.builder.paginas.inicio.secciones` (composición
  de bloques) y `template_config.editables[<templateId>]` (overrides de contenido/estilo de los
  demás templates fijos). Ver §2.

Nota: como cada tenant tiene su propia DB, `tienda_config` es literalmente una tabla de **una
sola fila** (`id INTEGER PRIMARY KEY DEFAULT 1`) — no hace falta `OrganizationId` porque el
aislamiento ya lo da la base física.

### 1.3 Placas para redes sociales (NO es theming de sitio — mencionado para descartarlo)
`backend/migrations/058_placas_plantillas.sql` crea `placas_plantillas` (recetas de imágenes
para Instagram/WhatsApp: variante + colores + fuentes + textos con placeholders). Es un
generador de piezas gráficas de marketing, un dominio de negocio aparte; no aporta al
theming del sitio.

## 2. Qué se puede personalizar exactamente

Fuente de verdad: `storefront/src/config/templatesEditables.js` (~1700 líneas, duplicado en
`mobile-cap/src/config/`). El comentario de cabecera lo llama **"el contrato entre el editor y
el render"**: el editor arma el formulario iterando esta config, y el storefront resuelve cada
valor con `valorDe(...)` usando el mismo default — no hay pantalla de edición por template,
es data-driven.

### 2.1 Tipos (JSDoc, JS plano — no hay TS)
```js
/**
 * @typedef {'texto'|'textarea'|'imagen'|'color'|'numero'|'bool'|'select'|'icono'|'lista'} TipoCampo
 *
 * @typedef {Object} CampoEditable
 * @property {string}   clave
 * @property {string}   label
 * @property {TipoCampo} tipo
 * @property {*}        [defecto]
 * @property {string}   [hint]
 * @property {number}   [max]
 * @property {Array<{id:string,label:string}>} [opciones] - sólo `select`
 * @property {CampoEditable[]} [campos] - sólo `lista`: forma de cada ítem
 *
 * @typedef {Object} ComponenteEditable
 * @property {string}  id
 * @property {string}  label
 * @property {string}  [hint]
 * @property {boolean} [ocultable]   - se puede apagar desde el editor
 * @property {boolean} [estilizable] - admite override de fondo/texto/acento
 * @property {boolean} [reordenable] - entra en el drag de orden del home
 * @property {CampoEditable[]} campos
 *
 * @typedef {Object} TemplateEditable
 * @property {string} nombre
 * @property {ComponenteEditable[]} componentes
 */
```

### 2.2 Forma real guardada en DB (comentario del mismo archivo)
```js
// tienda_config.template_config.editables[<templateId>]:
{
  orden: ['hero', 'coleccion', 'infostrip'],       // solo componentes reordenables
  componentes: {
    hero: {
      oculto: false,
      estilo: { fondo: '#fff', texto: '#111', acento: '#c8a27a' },
      valores: { titulo: 'Nueva temporada', beneficios: [{ icono: 'camion', texto: '…' }] },
    },
  },
}
```
Vacío = usa el default (no se persiste), así una tienda que nunca tocó un campo se actualiza
sola si cambia la redacción de fábrica.

### 2.3 Lista exhaustiva de lo personalizable
- **Colores:** por sección (`fondo`/`texto`/`acento`, campo `estilizable`) y **globales**
  (paleta completa aplicada a toda la tienda — ver §3).
- **Tipografía:** `fuenteTitulo` / `fuenteTexto` globales, mapeadas a un catálogo fijo de
  stacks de fuente (`servicios/aparienciaServicio.js`, `FUENTES`).
- **Textos:** todo campo `tipo: 'texto'|'textarea'` (títulos, subtítulos, CTAs, FAQ, etc.),
  con `max` de caracteres.
- **Imágenes:** campo `tipo: 'imagen'` (logo, favicon, hero, banners, fotos de producto en
  bloques tipo "comprar el look").
- **Íconos:** catálogo cerrado de 12 íconos (`cIconosEditables`, mapeados a `lucide-react`).
- **Listas:** campo `tipo: 'lista'` con sub-`campos` propios (ítems de beneficios, testimonios,
  preguntas frecuentes, tarjetas de un carrusel) — máximo configurable de ítems.
- **Booleanos:** `tipo: 'bool'` (ej. mostrar/ocultar envío, garantía, ubicaciones).
- **Ocultar/mostrar secciones enteras:** flag `ocultable` + `componentes[id].oculto`.
- **Reordenar secciones:** flag `reordenable` + array `orden` (drag & drop en el editor).
- **Radio de bordes global:** 3 presets (`recto` / `suave` / `redondeado`) que traducen a
  tríos de px concretos (`useEstiloRaizGlobal.js`).
- **Selección de plantilla completa** (`template_id`) — layout intercambiable, no solo skin
  (ver §5).
- **Composición libre de secciones** ("Diseño Libre" propiamente dicho): agregar/quitar/duplicar
  bloques de una paleta de ~16 tipos (Hero con 4 variantes, grilla/carrusel de productos,
  carrusel de categorías, bloque de texto, galería, franja de beneficios, testimonios, CTA,
  nav, footer, franja de anuncios, FAQ, video, reseñas, "comprar el look") — ver
  `docs/PROMPT-UX-DISENO-LIBRE.md` §2 y `storefront/src/templates/custom/`.
- **Multi-página:** modelo v2 con `paginas.inicio.secciones` (extensible a otras páginas, no
  solo home).

## 3. Cómo se aplica en runtime

**No hay CSS-in-JS ni ThemeProvider de una librería.** El mecanismo es CSS custom properties
inyectadas por JS directamente sobre `document.documentElement`, con React Context como capa
de distribución del dato (no del cálculo de estilo).

### 3.1 Panel admin (tema simple, `tenant_themes`)
`frontend/src/servicios/temaServicio.js`:
```js
export async function aplicarTemaAsync(eTema) {
    const iRoot = document.documentElement;
    Object.entries(eTema).forEach(([cClave, cValor]) => {
        if (cValor) iRoot.style.setProperty(`--${cClave}`, cValor);
    });
}
export function reiniciarTema() {
    document.documentElement.style.cssText = '';
}
export function alternarModoOscuro(eOscuro) {
    if (eOscuro) document.documentElement.setAttribute('data-theme', 'dark');
    else document.documentElement.removeAttribute('data-theme');
}
```
`cargarTemaAsync()` hace `fetch('/api/config/tema', { headers: { 'X-Tenant-ID': slug } })` y
llama a `aplicarTemaAsync`. Todo el CSS del admin usa `var(--color-primary)` etc. (tokens
propios, sin librería — confirmado también en `docs/PROMPT-UX-DISENO-LIBRE.md` §6: "Sin
librerías de UI pesadas... CSS variables propias").

### 3.2 Storefront público — `StoreConfigContext.jsx` (el "ThemeProvider" real)
`storefront/src/contextos/StoreConfigContext.jsx` (200 líneas) es un Context de React que:
- En SSR (Astro), lee `window.__INITIAL_STORE_CONFIG__` inyectado por el servidor (evita el
  parpadeo/flash de tema por defecto).
- Si no hay SSR, hace `fetch(/api/tienda-config?tenant=...)` con `X-Tenant-ID`.
- Expone `{ brand, navItems, copy, theme, templateId, modules, locations, templateConfig,
  apariencia, moneda, locale, catalogoModo, loading }` vía `useStoreConfig()`.
- Aplica efectos secundarios de branding directamente al DOM: `document.title`,
  `<link rel="icon">` (favicon dinámico con fallback a logo), `--sf-logo-height`.
- Sanea URLs de branding contra XSS (`sanearBranding`, evita `javascript:` en `href` porque en
  modo path-based varias tiendas comparten origen).
- Sincroniza un singleton externo de formato de moneda (`setConfigMoneda`) **en el render, no
  en un `useEffect`** — comentario explícito en el código explicando un bug real que esto
  arregló (mutar un singleton no re-renderiza).

### 3.3 Cálculo de CSS vars para "Diseño Libre" — `useEstiloRaizGlobal.js`
`storefront/src/templates/custom/useEstiloRaizGlobal.js` es la pieza más reutilizable del
sistema: un hook de React **puramente funcional** (sin red, sin negocio de e-commerce) que:
- Mezcla colores hacia blanco/negro para derivar variantes claras/oscuras (`mezclar`).
- Calcula el color de texto legible sobre un acento por contraste WCAG (`tintaSobre` /
  `parsearColor`, en `storefront/src/utils/contraste.js`).
- Aplica precedencia en cascada: paleta global → colores deducidos de la home (por "voto de
  mayoría" entre las secciones ya pintadas) → colores sueltos elegidos a mano → override por
  sección/bloque.
- Devuelve un objeto `style` con ~15 CSS custom properties (`--sf-bg`, `--sf-text`,
  `--sf-accent`, `--sf-accent-light`, `--sf-radius`, `--sf-font-heading`, etc.) que se aplica
  inline en el wrapper raíz `.sfc-root` — toda la tienda hereda por cascada CSS.

### 3.4 Live preview vía `postMessage` + iframe
El editor del admin no renderiza un mock — embebe el **storefront real** en un `<iframe>` con
`?preview_token=...`. `usePreviewBuilder.js` escucha `postMessage` para recibir el borrador en
vivo (composición aún no guardada) y lo usa como fuente de verdad por encima de lo persistido;
`usePreviewSeleccion.js` resalta la sección que el comerciante tiene seleccionada en el panel;
`usePreviewSoloLectura.js` deshabilita navegación/compra real dentro del iframe. Confirmado
también en `docs/PROMPT-UX-DISENO-LIBRE.md` §6: *"Vista previa: es un iframe de la tienda real
que recibe los cambios por postMessage (no es un mockup)"*.

## 4. ¿Hay editor visual con preview en vivo?

Sí — es el foco central del sistema, no un afterthought. `docs/PROMPT-UX-DISENO-LIBRE.md` (brief
de rediseño UX fechado, describe el estado funcional actual) documenta:
- Paleta de ~16 tipos de sección para agregar (con selección múltiple).
- Lista de secciones actuales con reordenar/duplicar/borrar.
- Panel de edición de la sección seleccionada, separado en **Contenido** vs **Estilo**.
- Paleta de colores global (~8 combinaciones predefinidas) aplicada a toda la página.
- **"Ideas hechas" (presets):** diseños prearmados que el usuario carga como punto de partida,
  lista que "crece sola".
- Preview en vivo en iframe con toggle Escritorio/Móvil.
- Guardar / Ver mi tienda.
El propio brief admite que hoy el preview de esos presets es "un esquema tipo wireframe" (no
screenshot real) y que la experiencia mobile completa "necesita diseño propio" — es decir, el
editor **funciona pero su UI está en proceso de pulido**, no es un admin panel plano sin preview.

## 5. Sistema de templates/layouts intercambiables

Es real y extenso, no theming superficial nada más. `storefront/src/registry.js` mapea
`template_id` a **22 layouts de rubro** (`automotriz-clasico`, `automotriz-premium`,
`blanqueria-1/2/3`, `indumentaria-1/2`, `bazar-1/2`, `multirubro-1`, `futbol-1`, `regalos-1`,
`billetera-1`, `casablanca-1`, `automotriz-sc`, `adonaid-1`, `ropa-1/2/3`, `ascender-1`,
`comida-1/2`, `forrajeria-1`) más `custom` (la entrada `'diseño-libre'`) — cada uno un chunk
lazy-loaded (`React.lazy`) con su propio `index.jsx`, code-splitting automático de Vite:
```js
export const registry = {
  'automotriz-clasico': lazy(() => import('./templates/automotriz-clasico/index.jsx')),
  ...
  'diseño-libre':       lazy(() => import('./templates/custom/index.jsx')),
};
export const TEMPLATE_FALLBACK = 'automotriz-clasico';
```
Los templates fijos (`automotriz-*`, `ropa-*`, etc.) traen **su propio Hero/Collection/Footer
prearmados**, y el comercio solo edita contenido/estilo vía `templatesEditables.js` (§2). El
template `custom` (`storefront/src/templates/custom/index.jsx`) es el motor genérico de
"Diseño Libre": no tiene layout fijo, lee `templateConfig.builder.paginas.inicio.secciones` y
renderiza cada bloque en orden vía `obtenerSeccion(tipo)` — "la composición es la fuente de
verdad: no hay lógica de negocio específica de rubro acá" (comentario de cabecera del archivo).
En resumen: conviven **dos modelos de personalización** — plantillas de rubro con theming
superficial (rápido, opinado) y un constructor libre de secciones (lento, total libertad) —
seleccionables por tenant vía `template_id`.

## 6. Stack — confirmado por código

| | stock-product (storefront/frontend) | gestion-alquileres-saas |
|---|---|---|
| React | 18.3 (`react ^18.3.0` / `^18.3.1`) | 19 |
| TypeScript | No — 0 archivos `.tsx`, todo `.jsx` con JSDoc | Sí, strict, sin `any` |
| Build | Vite 5/6 (storefront también Astro 6 para SSR) | Vite |
| Estilos | Tailwind 3.4 (utilidades) + CSS custom properties propias (sin librería de theming) | Sin librería de theming aún |
| Theming | Sin dependencia — `document.documentElement.style.setProperty` + Context | — |
| Estado global | React Context (`StoreConfigContext`), sin Redux/Zustand | — |
| Persistencia config | Postgres, 1 DB por tenant, columnas `TEXT`/`JSONB` en `tienda_config` | Postgres compartida + EF Core, filtro `OrganizationId` |

No hay `styled-components`, `zustand`, `emotion` ni ningún CSS-in-JS en ninguna de las tres apps
front de stock-product — confirmado en ambos `package.json` y en el propio brief de diseño
(§6 del documento: *"Sin librerías de UI pesadas... El sistema de estilos del admin son CSS
variables propias"*).

## 7. SSR, subdominios, dominios y client-side

Hay **tres modos de resolución de tenant** simultáneos, en este orden de prioridad
(`storefront/src/config/tenant.js`):
1. `VITE_TENANT_ID` (env var) — deploy dedicado por dominio propio/build por cliente.
2. Subdominio — `mi-negocio.dominio.com`.
3. Path-based — `dominio.com/mi-negocio` (estilo Pency), solo en dominios apex/localhost.

El storefront **SÍ tiene SSR real** vía Astro (`@astrojs/node`, adapter `node`), no es todo
client-side: `storefront/src/middleware.js` intercepta el request en el servidor, detecta el
slug del path (o subdominio) y lo inyecta como header `x-tenant-id` para que
`lib/tenantApi.ts` resuelva el mismo tenant que resolvería el cliente — con rewrite (no
redirect), así el `BrowserRouter` hidrata con el basename correcto. El HTML de respuesta se
sirve con `Cache-Control: no-store` explícito para evitar que visores in-app (WhatsApp,
Instagram) cacheen una versión vieja de la tienda tras un deploy; los assets con hash de
`_astro/*` sí cachean largo porque son inmutables. La config inicial (brand/theme/templateId)
se inyecta como `window.__INITIAL_STORE_CONFIG__` para hidratación sin flash de defaults.

## Veredicto de portabilidad

**El ENFOQUE es reutilizable sin copiar código; el código puntual de theming (no el de
e-commerce) sí es portable casi tal cual; el editor visual completo NO conviene portarlo ahora.**

- **¿Se puede reutilizar el ENFOQUE?** Sí, y es la parte de más valor: (1) separar
  `BrandingConfig` (colores/fuentes/logo, pocos campos, tabla propia) de
  `LayoutConfig`/`SectionsConfig` (JSON libre, versionado por template); (2) aplicar el tema
  como **CSS custom properties inyectadas en runtime** sobre el elemento raíz, no como
  CSS-in-JS ni theming de una librería de componentes; (3) un Context de React que carga la
  config del tenant una vez, la memoiza y expone hooks (`useTenantTheme`); (4) precedencia en
  cascada (global → derivado → override puntual) para que cambiar un color no obligue a tocar
  cada sección.

- **¿Hay código copiable/adaptable tal cual?** Sí, tres piezas concretas, todas JS puro sin
  acoplamiento a e-commerce (portan a TS con tipado mínimo):
  - La lógica de `useEstiloRaizGlobal.js` (mezcla de colores, contraste WCAG, generación del
    objeto `style` con CSS vars) — es matemática de color + memoización, cero dependencia de
    dominio.
  - `utils/contraste.js` (`tintaSobre`, `parsearColor`) — cálculo de contraste de texto sobre
    color de fondo, reutilizable en cualquier UI.
  - El patrón de `temaServicio.js` (`aplicarTemaAsync`/`reiniciarTema`/`alternarModoOscuro` vía
    `document.documentElement.style.setProperty` y atributo `data-theme`) — es la forma más
    simple posible de aplicar un tema sin librería, y ya funciona en claro/oscuro.

- **¿Conviene rehacerlo desde cero?** El **editor visual de secciones completo** (builder tipo
  Tiendanube con drag & drop, preview vía iframe/postMessage, 16 tipos de bloque, multi-página)
  NO conviene portarlo — es una inversión grande (meses, según el propio historial de fases del
  proyecto: "Fase 1 fundaciones", "Fase 2 UI de composición", "Fase 3 builder visual") que
  resuelve un problema de e-commerce (armar una landing de tienda con hero/carrusel de
  productos/testimonios) que un SaaS de alquileres no tiene: el portal de un inquilino o el
  dashboard de una inmobiliaria no necesita "secciones arrastrables", necesita **branding
  consistente** (logo, colores, tipografía de la inmobiliaria) sobre pantallas ya diseñadas.
  Para alquileres alcanza con el nivel de personalización de `tenant_themes` (§1.1), NO con el
  nivel de `template_config.builder` (§2). Rehacer esa porción simple desde cero es más barato
  que adaptar el builder completo.

- **Específico de stock-product (NO portar):** el registro de 22 templates de rubro
  (`registry.js`), los ~16 tipos de sección de e-commerce (carrusel de productos, "comprar el
  look", grilla de productos), `templatesEditables.js` completo (está descrito en términos de
  componentes de tienda), el modelo de 1-DB-por-tenant, la resolución de tenant por
  subdominio/path para un storefront público (un SaaS B2B de alquileres normalmente no expone
  storefronts públicos por tenant del mismo modo), y todo lo de Astro SSR/`postMessage`/iframe
  del builder.

- **Genérico (SÍ portable a cualquier SaaS multi-tenant):** la separación
  branding-simple-vs-config-libre en dos tablas, el mecanismo de CSS custom properties +
  `data-theme` para claro/oscuro, el Context de React como capa de distribución (no de
  cálculo), la función de mezcla de color + contraste WCAG, y el principio de "el default no
  se persiste" (para poder cambiar el default de fábrica sin migrar filas existentes).

### Cómo empezar a portarlo a gestion-alquileres-saas (accionable)

1. **Nueva tabla `TenantThemes`** (Domain/Infrastructure, EF Core) con `OrganizationId` (Guid,
   FK + índice único) y columnas planas espejando `tenant_themes` de stock-product:
   `PrimaryColor`, `SecondaryColor`, `BgBase`, `TextPrimary`, `BorderColor`, `FontHeading`,
   `FontBody`, `RadiusDefault`, `LogoUrl`, `DarkModeEnabled` — todas con default razonable, sin
   `NOT NULL` a secas para poder tener fila ausente = usar defaults del sistema.
2. **No copiar el `template_config.builder` JSONB de secciones libres** — no hay caso de uso de
   "armar una landing por bloques" en este dominio; si aparece a futuro (ej. portal público de
   la inmobiliaria), evaluarlo como iniciativa aparte, no como parte de este porteo.
   Nota: `OrganizationId` YA cubre el aislamiento multi-tenant vía el filtro global de EF Core
   existente — no hace falta ningún mecanismo nuevo de aislamiento para esta tabla.
3. **Endpoint** `GET /api/organizations/{id}/theme` (o extraído del JWT como el resto del
   sistema) que devuelve el `TenantThemeDto` — Query con MediatR, sin lógica en el controller,
   siguiendo el patrón `{Accion}{Recurso}Query`/Handler ya establecido en el proyecto.
4. **Hook `useTenantTheme()`** en `web/src/shared/` (no en un feature) que hace fetch una vez,
   memoiza, y expone `{ theme, loading }` — calco de `StoreConfigContext.jsx` pero sin todo lo
   de e-commerce (sin `navItems`, `copy`, `templateId`, `modules`).
5. **Función pura `aplicarTema(theme: TenantTheme): void`** en TypeScript (`web/src/shared/`)
   que haga `document.documentElement.style.setProperty('--color-primary', theme.primaryColor)`
   por cada campo — puerto directo de `temaServicio.js`, tipado. Reservar un componente
   `<ThemeProvider>` fino que solo llama a este efecto al montar/cambiar `theme`, sin lógica de
   negocio adentro.
6. **Portar `contraste.ts`** (función `tintaSobre(colorHex): string`) para calcular el color de
   texto legible sobre el color de acento elegido por la inmobiliaria (útil para botones,
   badges de estado de contrato, etc.) — es ~30 líneas de matemática de color, sin
   dependencias, alto valor.
7. **Variables CSS ya declaradas como tokens** en el CSS global del admin (`--color-primary`,
   `--color-bg`, `--color-text`, `--radius-default`, etc.) para que exista algo que sobreescribir
   — hoy el proyecto no tiene ninguna, es prerrequisito antes de que el theming tenga efecto
   visual.
8. **Panel de administración simple** (una pantalla en el portal admin, NO un editor visual):
   selector de color con preview inmediato (aplicar la CSS var al vuelo antes de guardar, como
   hace `aplicarTemaAsync` en stock-product), input de URL de logo, selector de fuente de una
   lista corta cerrada (2-3 opciones tipo `FUENTES` de `aparienciaServicio.js`) — sin drag & drop,
   sin secciones, sin preview en iframe: eso es la parte cara del sistema de stock-product y no
   aporta al caso de uso de alquileres.

## Archivos clave citados (rutas absolutas)

- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\backend\migrations\003_theme_system.sql`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\backend\migrations\001_schema_inicial.sql`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\backend\migrations\006_template_plan.sql`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\backend\migrations\026_template_config.sql`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\storefront\src\config\templatesEditables.js`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\storefront\src\contextos\StoreConfigContext.jsx`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\storefront\src\templates\custom\index.jsx`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\storefront\src\templates\custom\useEstiloRaizGlobal.js`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\storefront\src\registry.js`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\storefront\src\config\tenant.js`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\storefront\src\middleware.js`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\frontend\src\servicios\temaServicio.js`
- `D:\Proyectos_Propios\Trabajo\stock-product\stock-product\docs\PROMPT-UX-DISENO-LIBRE.md`
