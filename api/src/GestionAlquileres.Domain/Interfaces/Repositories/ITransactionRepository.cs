using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

/// <summary>
/// Lo cobrado en el período para un contrato de un propietario, con los datos de la propiedad que
/// la liquidación necesita. Es una proyección de lectura: no se materializa ninguna entidad.
/// </summary>
public record OwnerCollectedRow(
    Guid PropertyId,
    string PropertyAddress,
    decimal? CommissionPct,
    Guid ContractId,
    decimal Collected);

/// <summary>
/// Un cargo vencido e impago que ya devenga punitorio, junto con la tasa y la tolerancia del
/// contrato. Es una proyección de lectura para el devengamiento diario: sin ella el job tendría que
/// traer los contratos activos y consultar los cargos de cada uno, uno por uno.
/// </summary>
public record OverdueChargeRow(
    Guid ChargeId,
    Guid OrganizationId,
    Guid ContractId,
    DateOnly DueDate,
    decimal DailyRate,
    int GraceDays);

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct);
    /// <summary>
    /// One page of the org's transactions (optionally filtered by type and tenant/address/notes), the
    /// total count, and the net cash balance over the WHOLE filtered set (credits − owed charges).
    /// </summary>
    Task<(IReadOnlyList<Transaction> Items, int Total, decimal NetBalance)> GetPagedAsync(
        TransactionType? type, string? search, int page, int pageSize, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetByContractAsync(Guid contractId, CancellationToken ct);

    /// <summary>
    /// Lo cobrado por contrato para las propiedades de un propietario, dentro del período.
    ///
    /// Reemplaza el recorrido anidado de la liquidación, que hacía una consulta por propiedad y
    /// otra por contrato, y encima traía TODAS las transacciones de cada contrato para después
    /// filtrar el período en memoria: un propietario con 40 propiedades y cinco años de historial
    /// disparaba 81 idas a la base y materializaba unas 6.000 entidades para sumar 40 filas.
    /// </summary>
    Task<IReadOnlyList<OwnerCollectedRow>> GetCollectedByOwnerAsync(
        Guid ownerId, DateOnly periodFrom, DateOnly periodTo, CancellationToken ct);
    /// <summary>
    /// Cargos impagos de un contrato (RentCharge/ManualDebit/LateFee, Status=Pending), del más
    /// viejo al más nuevo — para imputar un pago. Incluye los punitorios: son deuda del inquilino
    /// como cualquier otro cargo, y dejarlos fuera haría que un pago que alcanza para todo dejara
    /// el punitorio pendiente para siempre.
    /// </summary>
    Task<IReadOnlyList<Transaction>> GetPendingChargesAsync(Guid contractId, CancellationToken ct);

    /// <summary>
    /// Cargos vencidos e impagos de contratos activos con punitorio pactado, en TODAS las
    /// organizaciones. Sólo la usa el devengamiento programado, por eso pasa por encima del filtro
    /// multi-tenant; cada fila trae su OrganizationId para que el trabajo siga siendo por tenant.
    /// </summary>
    Task<IReadOnlyList<OverdueChargeRow>> GetOverdueChargesForLateFeeRawAsync(
        DateOnly asOf, CancellationToken ct);

    /// <summary>Una transacción por id, con la organización explícita en vez del filtro global.</summary>
    Task<Transaction?> GetByIdRawAsync(Guid id, Guid organizationId, CancellationToken ct);

    /// <summary>El punitorio vivo de un cargo, si ya se devengó alguna vez. Organización explícita.</summary>
    Task<Transaction?> GetLateFeeForChargeRawAsync(
        Guid chargeId, Guid organizationId, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Transaction transaction, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
