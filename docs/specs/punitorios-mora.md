# Punitorios e intereses por mora

**Estado:** implementado (backend + panel). Pendiente de desplegar y verificar en el demo. · **Bloque 1 de `PENDIENTES.md`** · 2026-09-09

## Por qué

73% de los hogares inquilinos acumula deudas (Encuesta Nacional Inquilina, jun-2026). El producto
que gana no es el que calcula mejor el ICL —eso ya lo hacen todos— sino el que **reduce días de
mora**. Barreeo vende explícitamente "punitorios automáticos calculados día a día"; hoy nuestro
`TransactionType` no tiene siquiera un tipo para representarlos.

## Decisiones

| Decisión | Qué se eligió | Por qué |
|---|---|---|
| Forma de la tasa | **% diario fijo** por contrato (`0,1` = 0,1 % por día) | Es como se redacta el punitorio en el contrato de locación argentino, y es la promesa de la competencia ("día a día"). Un solo número, sin unidad ambigua. |
| Asiento en la cuenta | **Una línea que crece**: un punitorio por cargo vencido, vinculado a él, cuyo importe se recalcula cada día | La cuenta queda corta y legible, y el saldo refleja siempre lo adeudado **hoy**. La alternativa (un asiento cerrado por mes) llena la cuenta de filas y deja el mes en curso incompleto. |
| Capitalización | **No hay.** El punitorio se devenga siempre sobre el capital del cargo, nunca sobre punitorios ya devengados | Anatocismo. El art. 770 CCyC sólo lo admite en supuestos tasados; un SaaS no puede asumir que el contrato lo pactó. |
| Días de gracia | Campo propio por contrato, default `0` | Muchos contratos pactan una tolerancia ("hasta el día 10 sin recargo"). Sin el campo habría que falsear el vencimiento. |
| Congelado | El punitorio deja de crecer **el día en que se salda el cargo**, no el día en que corre el job | Si sólo lo congelara el job diario, el inquilino que paga hoy vería mañana un día extra de punitorio que no debe. |

## Modelo

### `Contract`
- `LateFeeDailyRate` (`decimal?`, precisión 6,4) — porcentaje **diario**. `null` o `0` ⇒ el contrato
  no devenga punitorios. 6,4 admite hasta 99,9999 % diario: holgado y a la vez acota una carga errónea.
- `LateFeeGraceDays` (`int`, default `0`, 0-90) — días de tolerancia contados **desde el vencimiento**.

### `Transaction`
- `TransactionType.LateFee` — **se agrega al final del enum** (`= 4`). Correr los valores existentes
  reinterpretaría toda la tabla `transactions` ya persistida.
- `IsCharge` pasa a incluir `LateFee`: el punitorio es deuda del inquilino y tiene que sumar al saldo
  y entrar en la imputación de pagos.
- `RelatedTransactionId` (`Guid?`) — el cargo que originó el punitorio. Único entre los no nulos:
  **un solo punitorio vivo por cargo**, que es lo que hace idempotente al job.
- `AccruedThroughDate` (`DateOnly?`) — último día devengado. Es el testigo de idempotencia: si ya
  llega a hoy, la corrida no toca nada.

## Cálculo

```
diasDeMora  = max(0, hoy − (vencimiento + diasDeGracia))
punitorio   = redondear(capitalDelCargo × tasaDiaria / 100 × diasDeMora, 2)
```

`Domain/Billing/LateFeeCalculator.cs`, función pura y testeada aparte del job.

## Devengamiento

`LateFeeAccrualService` (Application) concentra el alta/actualización del punitorio de un cargo
hasta una fecha dada. Lo llaman dos caminos, y por eso no vive dentro del job:

1. **`LateFeeAccrualJob`**, diario a las 05:00 (antes del job de vencimientos de las 10:00).
   Recorre los cargos `Pending` vencidos de contratos activos con tasa configurada, en su propio
   scope por cargo, con `[DisableConcurrentExecution]`, siguiendo el patrón de
   `MonthlyRentAdjustmentJob`.
2. **Los dos caminos de cobro** (`RegisterPaymentCommandHandler`, `MarkTransactionPaidCommandHandler`)
   devengan hasta la fecha del pago **antes** de imputar, para que el punitorio quede congelado en el
   día correcto y para que el propio punitorio entre en la cola de imputación.

El punitorio nace `Pending`, con `Period` del cargo que lo originó y `DueDate` = hoy, de modo que
figura como exigible pero no como vencido.

## Alcance de esta entrega

- [x] Dominio: enum, campos de `Contract` y `Transaction`, `LateFeeCalculator`
- [x] Persistencia: configuración EF, índice único parcial, migración `20260909135748_AddLateFeeAccrual`
- [x] Application: `LateFeeAccrualService`, enganche en los dos caminos de cobro, campos en
      create/update de contrato + validaciones
- [x] API: job `late-fee-accrual` registrado en Hangfire, diario a las 05:00
- [x] Web: campos en el alta de contrato, punitorio en el detalle, tipo `LateFee` en las pantallas
      de transacciones (panel e inquilino)
- [x] Tests: 23 casos nuevos — cálculo, idempotencia, no capitalización, congelado al pagar,
      imputación con punitorio, días de gracia. Suite: 358/359 (el rojo es
      `StorageProviderValidationTests`, preexistente y ajeno a este bloque)

**No hay pantalla de edición de contrato**: el hook `useUpdateContract` existe pero ninguna vista lo
usa, así que el punitorio hoy se pacta en el alta. Cambiarlo en un contrato ya cargado requiere la
API. Queda anotado en `PENDIENTES.md`.

## Fuera de alcance (a `PENDIENTES.md`)

- Tasa variable atada a la tasa activa del Banco Nación.
- Tope de punitorio (algunos contratos pactan un máximo acumulado).
- Planes de pago / refinanciación de la deuda.
- Aviso automático de mora al inquilino — depende del canal de WhatsApp, que es otro bloque.
- **A quién le corresponde el punitorio.** Hoy el cobro del punitorio genera un `Payment` como
  cualquier otro, así que entra en la liquidación al propietario y se le descuenta comisión. En la
  práctica argentina el punitorio suele ser del propietario (indemniza el retardo), pero hay
  administraciones que se lo quedan. Si hay que distinguirlo, es una decisión de negocio y un campo
  más en la liquidación, no un cambio del devengamiento.
