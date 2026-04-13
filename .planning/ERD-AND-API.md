# ERD + API Architecture — Gestión Alquileres SaaS

## Esquema de Base de Datos (PostgreSQL)

### ERD — Relaciones principales

```
Organizations (1) ──< Users (M)
Organizations (1) ──< Properties (M)
Organizations (1) ──< AppTenants (M)
Organizations (1) ──< Contracts (M)

Properties (1) ──< Contracts (M)
AppTenants (1) ──< Contracts (M)

Contracts (1) ──< RentHistories (M)
Contracts (1) ──< Transactions (M)
Contracts (1) ──< Documents (M)

IndexValues ──> RentHistories (índice usado en el ajuste)
Users ──> Documents (uploadedBy)
RentHistories ──> Transactions (cargo originado por el ajuste)
```

### Tabla: organizations
```sql
CREATE TABLE organizations (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name            VARCHAR(200) NOT NULL,
    slug            VARCHAR(100) NOT NULL UNIQUE,  -- para subdomain routing
    plan            VARCHAR(20) NOT NULL DEFAULT 'free',  -- free, basic, pro
    is_active       BOOLEAN NOT NULL DEFAULT true,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### Tabla: users
```sql
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    email           VARCHAR(320) NOT NULL,
    password_hash   VARCHAR(500) NOT NULL,
    first_name      VARCHAR(100) NOT NULL,
    last_name       VARCHAR(100) NOT NULL,
    role            VARCHAR(20) NOT NULL,  -- Admin, Staff, Tenant
    is_active       BOOLEAN NOT NULL DEFAULT true,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    UNIQUE(organization_id, email)  -- email único por organización
);
CREATE INDEX idx_users_org ON users(organization_id);
```

### Tabla: properties
```sql
CREATE TABLE properties (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    street          VARCHAR(200) NOT NULL,
    number          VARCHAR(20) NOT NULL,
    floor           VARCHAR(20),
    unit            VARCHAR(20),
    city            VARCHAR(100) NOT NULL,
    province        VARCHAR(100) NOT NULL DEFAULT 'Buenos Aires',
    property_type   VARCHAR(30) NOT NULL,  -- Department, House, Commercial, Garage
    bedrooms        SMALLINT,
    bathrooms       SMALLINT,
    surface_m2      DECIMAL(8,2),
    is_active       BOOLEAN NOT NULL DEFAULT true,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_properties_org ON properties(organization_id);
```

### Tabla: app_tenants (perfil de inquilino)
```sql
CREATE TABLE app_tenants (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id UUID NOT NULL REFERENCES organizations(id),
    user_id         UUID REFERENCES users(id),  -- nullable: puede no tener portal
    first_name      VARCHAR(100) NOT NULL,
    last_name       VARCHAR(100) NOT NULL,
    dni             VARCHAR(20),
    email           VARCHAR(320),
    phone           VARCHAR(50),
    is_active       BOOLEAN NOT NULL DEFAULT true,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_tenants_org ON app_tenants(organization_id);
```

### Tabla: contracts ⭐ (núcleo del sistema)
```sql
CREATE TABLE contracts (
    id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id         UUID NOT NULL REFERENCES organizations(id),
    property_id             UUID NOT NULL REFERENCES properties(id),
    tenant_id               UUID NOT NULL REFERENCES app_tenants(id),
    start_date              DATE NOT NULL,
    end_date                DATE NOT NULL,
    initial_rent_amount     DECIMAL(18,2) NOT NULL,
    current_rent_amount     DECIMAL(18,2) NOT NULL,
    currency                VARCHAR(3) NOT NULL DEFAULT 'ARS',
    adjustment_type         VARCHAR(20) NOT NULL,  -- ICL, IPC, Manual, None
    adjustment_frequency    VARCHAR(20) NOT NULL,  -- Monthly, Quarterly, Annual
    day_of_month_due        SMALLINT NOT NULL CHECK (day_of_month_due BETWEEN 1 AND 28),
    grace_period_days       SMALLINT NOT NULL DEFAULT 5,
    status                  VARCHAR(20) NOT NULL DEFAULT 'Active',  -- Active, Expired, Terminated, Pending
    notes                   TEXT,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_contracts_org ON contracts(organization_id);
CREATE INDEX idx_contracts_tenant ON contracts(tenant_id);
CREATE INDEX idx_contracts_property ON contracts(property_id);
CREATE INDEX idx_contracts_status ON contracts(organization_id, status);
```

### Tabla: index_values ⭐ (NO tiene organization_id — datos globales BCRA)
```sql
CREATE TABLE index_values (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    index_type      VARCHAR(10) NOT NULL,  -- ICL, IPC, UVA
    period          DATE NOT NULL,          -- Primer día del mes (ej: 2025-03-01)
    value           DECIMAL(18,6) NOT NULL,
    variation_pct   DECIMAL(10,6),          -- % de variación respecto al período anterior
    source          VARCHAR(20) NOT NULL,   -- BCRA, INDEC
    fetched_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    is_manual_override BOOLEAN NOT NULL DEFAULT false,
    UNIQUE(index_type, period)              -- Un valor por tipo e período
);
CREATE INDEX idx_indexes_type_period ON index_values(index_type, period);
```

### Tabla: rent_history ⭐ (log auditado de cada ajuste)
```sql
CREATE TABLE rent_history (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id     UUID NOT NULL REFERENCES organizations(id),
    contract_id         UUID NOT NULL REFERENCES contracts(id),
    period              DATE NOT NULL,           -- Período del ajuste (primer día del mes)
    base_amount         DECIMAL(18,2) NOT NULL,  -- Alquiler ANTES del ajuste
    adjustment_amount   DECIMAL(18,2) NOT NULL,  -- Diferencia (puede ser negativa en descuento)
    adjusted_rent_amount DECIMAL(18,2) NOT NULL, -- Alquiler DESPUÉS del ajuste
    index_id            UUID REFERENCES index_values(id),  -- null si es ajuste manual
    adjustment_type     VARCHAR(20) NOT NULL,
    adjustment_pct      DECIMAL(10,6),           -- % aplicado (para auditoría)
    notes               TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_rent_history_contract ON rent_history(contract_id);
CREATE INDEX idx_rent_history_org ON rent_history(organization_id);
```

### Tabla: transactions
```sql
CREATE TABLE transactions (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id     UUID NOT NULL REFERENCES organizations(id),
    contract_id         UUID NOT NULL REFERENCES contracts(id),
    rent_history_id     UUID REFERENCES rent_history(id),  -- null si es transacción manual
    transaction_type    VARCHAR(20) NOT NULL,  -- RentCharge, ManualCharge, Penalty, Discount, Payment
    amount              DECIMAL(18,2) NOT NULL,  -- Positivo = cargo, negativo = crédito/pago
    due_date            DATE,
    paid_date           DATE,
    status              VARCHAR(20) NOT NULL DEFAULT 'Pending',  -- Pending, Paid, Overdue, Cancelled
    notes               TEXT,  -- OBLIGATORIO para tipo Manual
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_transactions_contract ON transactions(contract_id);
CREATE INDEX idx_transactions_org ON transactions(organization_id);
CREATE INDEX idx_transactions_status ON transactions(organization_id, status);
```

### Tabla: documents
```sql
CREATE TABLE documents (
    id                  UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    organization_id     UUID NOT NULL REFERENCES organizations(id),
    contract_id         UUID NOT NULL REFERENCES contracts(id),
    transaction_id      UUID REFERENCES transactions(id),
    uploaded_by_user_id UUID NOT NULL REFERENCES users(id),
    file_name           VARCHAR(500) NOT NULL,
    storage_path        VARCHAR(1000) NOT NULL,  -- Path interno al storage (NUNCA exponer directamente)
    document_type       VARCHAR(30) NOT NULL,    -- Contract, Receipt, PaymentProof, ID, Other
    is_visible_to_tenant BOOLEAN NOT NULL DEFAULT false,  -- Privado por defecto
    content_type        VARCHAR(100) NOT NULL,
    size_bytes          BIGINT NOT NULL,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT now()
);
CREATE INDEX idx_documents_contract ON documents(contract_id);
CREATE INDEX idx_documents_org ON documents(organization_id);
CREATE INDEX idx_documents_visible ON documents(contract_id, is_visible_to_tenant);
```

---

## Fallo Crítico de Diseño que Evitar

**Anti-patrón detectado en el requerimiento:**
> "ajuste basado en ICL" — sin especificar el período de comparación

**El problema:** La Ley 27.737 (vigente desde Abril 2024) establece que el ICL se aplica
anualmente usando el promedio de los últimos 12 meses vs los 12 meses anteriores,
**no** el ICL de un mes vs el mismo mes del año anterior.

**Implementación correcta para ICL:**
```
Factor = Promedio(ICL[T-11..T]) / Promedio(ICL[T-23..T-12])
```

**Para el MVP** se puede usar la aproximación más simple (ICL_T / ICL_T-12) que es como
la mayoría de calculadoras de alquiler lo implementa, pero documentarlo como simplificación.
La fórmula exacta requiere 24 meses de datos históricos de ICL.

---

## Estructura de la API (.NET 8)

### Controllers y Endpoints

```
POST   /api/v1/auth/register-org          → Registrar organización + admin inicial
POST   /api/v1/auth/login                 → JWT para Admin/Staff
POST   /api/v1/auth/tenant-login          → JWT para Tenant (portal inquilino)
POST   /api/v1/auth/refresh               → Refresh token

GET    /api/v1/indexes?type=ICL&from=&to= → Listar índices por tipo y rango
POST   /api/v1/indexes/sync               → Sincronizar desde BCRA/INDEC (Admin)
GET    /api/v1/indexes/{type}/{period}    → Obtener índice específico

GET    /api/v1/properties                 → Listar propiedades del tenant
POST   /api/v1/properties                 → Crear propiedad
GET    /api/v1/properties/{id}            → Obtener propiedad
PUT    /api/v1/properties/{id}            → Actualizar propiedad

GET    /api/v1/tenants                    → Listar inquilinos del tenant
POST   /api/v1/tenants                    → Crear perfil de inquilino
GET    /api/v1/tenants/{id}               → Obtener inquilino
PUT    /api/v1/tenants/{id}               → Actualizar inquilino
POST   /api/v1/tenants/{id}/invite        → Invitar al portal

GET    /api/v1/contracts                  → Listar contratos (con filtros: status, tenant, property)
POST   /api/v1/contracts                  → Crear contrato
GET    /api/v1/contracts/{id}             → Obtener contrato con historial
PUT    /api/v1/contracts/{id}             → Actualizar contrato
POST   /api/v1/contracts/{id}/terminate   → Terminar contrato
POST   /api/v1/contracts/{id}/ajuste      → Calcular ajuste manual (Admin trigger)
GET    /api/v1/contracts/{id}/balance     → Obtener balance actual

GET    /api/v1/contracts/{id}/transactions → Listar transacciones
POST   /api/v1/contracts/{id}/transactions → Crear transacción manual (cargo/pago/descuento)
PATCH  /api/v1/transactions/{id}           → Actualizar estado de transacción

POST   /api/v1/contracts/{id}/documents   → Subir documento
GET    /api/v1/documents/{id}/url         → Obtener presigned URL (5 min)
PATCH  /api/v1/documents/{id}/visibility  → Toggle IsVisibleToTenant

# Portal Inquilino (auth separada, JWT con rol Tenant)
GET    /api/v1/portal/estado-cuenta       → Balance, próximo vencimiento, importe actual
GET    /api/v1/portal/documentos          → Solo IsVisibleToTenant = true
GET    /api/v1/portal/ajustes             → Historial de ajustes del contrato
POST   /api/v1/portal/comprobante         → Subir comprobante de pago
```

### Estrategia de Seguridad para Documentos

**Principio:** Storage path = secreto interno. Nunca sale de la API.

```
1. Admin sube archivo → API:
   a. Recibe multipart/form-data
   b. Genera UUID como nombre en storage: {orgId}/{contractId}/{uuid}.{ext}
   c. Sube al storage (MinIO/Azure) con ACL = private
   d. Guarda en Documents: storage_path = path interno, is_visible_to_tenant = false
   e. Retorna Document con ID, sin storage_path

2. Admin/Inquilino descarga:
   a. GET /api/v1/documents/{id}/url
   b. API verifica: JWT válido + OrganizationId del JWT coincide con el del documento
   c. Si es inquilino: verifica además que is_visible_to_tenant = true
   d. Genera presigned URL con 5 minutos de expiración
   e. Retorna { url: "https://storage.../...?signature=...&expires=..." }
   f. Cliente descarga directo desde storage usando la presigned URL

3. Lo que NUNCA ocurre:
   - storage_path nunca aparece en ningún response de la API
   - No hay endpoint que sirva el binario del archivo desde la API (evitar memory pressure)
   - No hay URLs públicas en el storage
```

---

## Lógica de Cálculo — Decisiones de Diseño

### ¿Por qué persistir índices antes de calcular?

**Problema real:** El BCRA publica el ICL mensualmente con ~10 días de retraso.
Si el scheduler corre el 1ro del mes y el ICL de ese mes no está disponible aún,
¿qué hacemos? Opciones:

1. **Fallar el job** — malo: bloquea todos los contratos ese mes
2. **Usar el último disponible** — peor: calcula con dato incorrecto silenciosamente
3. **Persistir el índice cuando aparece** → **calcular cuando el dato esté disponible** ✓

**Implementación:**
- SyncIndexJob corre todos los días a las 8am
- Cuando encuentra un índice nuevo, lo persiste y dispara `IndexPublicadoEvent`
- `AjusteMensualHandler` reacciona al evento y procesa contratos pendientes del período
- Los contratos que esperaban el índice se procesan automáticamente sin intervención manual

### Manejo del DNU 70/2023 y Ley 27.737

La ley de alquileres cambió múltiples veces. El campo `AdjustmentType` en Contract
permite manejar esto: cada contrato guarda QUÉ tipo de ajuste aplica.

| Período | Ley vigente | Tipo de ajuste |
|---------|-------------|----------------|
| Antes Oct 2023 | Ley 27.551 | IPC semestral |
| Oct 2023 - Abr 2024 | DNU 70/2023 | A elección (IPC o ICL) trimestral |
| Desde Abr 2024 | Ley 27.737 | ICL anual (o IPC a elección en algunos casos) |

El sistema soporta cualquier combinación via AdjustmentType + AdjustmentFrequency en el contrato.
