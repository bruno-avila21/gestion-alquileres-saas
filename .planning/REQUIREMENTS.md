# Requirements: Gestión Alquileres SaaS

**Defined:** 2026-04-12
**Core Value:** Cálculo automático de ajustes ICL/IPC con datos BCRA persistidos — confiable, auditable, multi-tenant

## v1 Requirements

### Infrastructure & Multi-tenancy

- [ ] **INFRA-01**: Sistema multi-tenant con OrganizationId como discriminador en DB compartida (PostgreSQL)
- [ ] **INFRA-02**: JWT incluye OrganizationId; global query filters EF Core aíslan datos por tenant
- [ ] **INFRA-03**: Estructura Clean Architecture: Domain / Application / Infrastructure / API
- [ ] **INFRA-04**: Hangfire configurado para scheduled jobs con persistencia en PostgreSQL
- [ ] **INFRA-05**: Serilog con structured logging para auditoría de cálculos y accesos

### Indexes (BCRA/INDEC)

- [ ] **IDX-01**: Servicio consume API BCRA para obtener valores ICL por período
- [ ] **IDX-02**: Servicio consume API INDEC para obtener valores IPC por período
- [ ] **IDX-03**: Valores de índices se persisten en tabla `Indexes` antes de usar en cálculos
- [ ] **IDX-04**: Fallback a último valor disponible en DB si API externa falla; log de warning
- [ ] **IDX-05**: Endpoint para sincronización manual de índices (trigger por admin)
- [ ] **IDX-06**: Endpoint para consultar índices históricos por tipo y rango de fechas

### Organizations & Users

- [ ] **ORG-01**: CRUD de Organizations (registro de inmobiliarias/administradores)
- [ ] **ORG-02**: Users con roles: Admin, Staff, Tenant; asociados a Organization
- [ ] **ORG-03**: Registro de organización crea usuario Admin inicial
- [ ] **ORG-04**: Autenticación JWT (email + password) para usuarios Admin/Staff
- [ ] **ORG-05**: Autenticación JWT separada para inquilinos (rol Tenant)

### Properties & Tenants

- [ ] **PROP-01**: CRUD de Properties (propiedades) vinculadas a Organization
- [ ] **PROP-02**: Propiedades con tipo (Department, House, Commercial), dirección, superficie
- [ ] **TNT-01**: CRUD de perfiles de Tenant (inquilinos) vinculados a Organization
- [ ] **TNT-02**: Tenant puede o no tener acceso al portal (campo UserId nullable)
- [ ] **TNT-03**: Invitar inquilino al portal vía email

### Contracts

- [ ] **CTR-01**: CRUD de Contracts vinculando Property + Tenant + Organization
- [ ] **CTR-02**: Contrato tiene: fechas, importe inicial, tipo de ajuste (ICL/IPC/Manual/None), frecuencia, día de vencimiento
- [ ] **CTR-03**: Estado de contrato: Active, Expired, Terminated, Pending
- [ ] **CTR-04**: Historial de cambios de importe de alquiler (RentHistory)
- [ ] **CTR-05**: Ajuste manual disponible con campo Notes obligatorio

### Rent Calculation

- [ ] **CALC-01**: Cálculo de ajuste ICL: `NuevoAlquiler = Actual × (ICL_T / ICL_T-12meses)` trimestral
- [ ] **CALC-02**: Cálculo de ajuste IPC: acumulación según frecuencia (mensual/trimestral/anual)
- [ ] **CALC-03**: Cálculo SOLO con índice persistido en DB — nunca live de API externa
- [ ] **CALC-04**: Scheduler Hangfire ejecuta ajustes automáticos según fecha de ajuste de cada contrato
- [ ] **CALC-05**: Log auditado de cada ajuste: índice usado, factor, montos anterior y nuevo

### Transactions

- [ ] **TRX-01**: Transactions de tipo: RentCharge, ManualCharge, Penalty, Discount, Payment
- [ ] **TRX-02**: Estado de transacción: Pending, Paid, Overdue, Cancelled
- [ ] **TRX-03**: Balance por contrato: suma de cargos - suma de pagos
- [ ] **TRX-04**: Inquilino puede registrar pago subiendo comprobante (foto/PDF)

### Documents

- [ ] **DOC-01**: Admin puede subir documentos (recibos, contratos) asociados a un Contract
- [ ] **DOC-02**: Cada documento tiene flag `IsVisibleToTenant` (default: false)
- [ ] **DOC-03**: Acceso a documentos SIEMPRE via presigned URLs con 5 min de expiración
- [ ] **DOC-04**: Admin puede cambiar IsVisibleToTenant de cualquier documento
- [ ] **DOC-05**: Inquilino puede subir comprobantes de pago (tipo PaymentProof)

### Tenant Portal

- [ ] **PRT-01**: Portal de inquilino con auth separada (JWT rol Tenant)
- [ ] **PRT-02**: Inquilino ve su estado de cuenta: balance actual, próximo vencimiento, importe
- [ ] **PRT-03**: Inquilino ve y descarga documentos donde IsVisibleToTenant = true
- [ ] **PRT-04**: Inquilino puede subir foto/PDF de comprobante de pago
- [ ] **PRT-05**: Inquilino ve historial de ajustes aplicados a su contrato

### Notifications

- [ ] **NOTF-01**: Email a inquilino cuando se aplica un ajuste ICL/IPC con detalle del nuevo importe
- [ ] **NOTF-02**: Email a inquilino cuando un documento es habilitado para su vista

## v2 Requirements

### Multi-currency
- **CURR-01**: Soporte para contratos en USD con tipo de cambio (BCRA oficial / blue)

### Advanced Features
- **ADV-01**: Firma electrónica de contratos (integración DocuSign o FirmaEN)
- **ADV-02**: Facturación electrónica AFIP para recibos de alquiler
- **ADV-03**: Reportes y exportación a PDF/Excel (estado de cuenta, histórico)
- **ADV-04**: App móvil (PWA o React Native)
- **ADV-05**: OAuth/SSO (Google, Microsoft) para login de admins

### Integrations
- **INT-01**: API pública para integración con sistemas externos de inmobiliarias
- **INT-02**: Webhook de notificaciones para eventos clave (ajuste aplicado, pago registrado)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Mobile app nativa | Web-first para MVP. PWA suficiente para v1. |
| OAuth/SSO | Email+password suficiente para v1. Complejidad adicional sin demanda validada. |
| Facturación AFIP | Alta complejidad regulatoria. Defer a v2 con validación de mercado primero. |
| Multi-DB tenancy (DB por tenant) | Mayor costo operacional. Discriminador suficiente para escala inicial. |
| Firma electrónica | Integración costosa. Flujo manual funciona para MVP. |
| Contabilidad full | Fuera del scope — gestión de alquileres, no ERP. |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| INFRA-01..05 | Phase 1 | Pending |
| ORG-01..05 | Phase 1 | Pending |
| IDX-01..06 | Phase 2 | Pending |
| PROP-01..02 | Phase 3 | Pending |
| TNT-01..03 | Phase 3 | Pending |
| CTR-01..05 | Phase 4 | Pending |
| CALC-01..05 | Phase 5 | Pending |
| TRX-01..04 | Phase 5 | Pending |
| DOC-01..05 | Phase 6 | Pending |
| PRT-01..05 | Phase 7 | Pending |
| NOTF-01..02 | Phase 8 | Pending |

**Coverage:**
- v1 requirements: 42 total
- Mapped to phases: 42
- Unmapped: 0 ✓

---
*Requirements defined: 2026-04-12*
*Last updated: 2026-04-12 after initial definition*
