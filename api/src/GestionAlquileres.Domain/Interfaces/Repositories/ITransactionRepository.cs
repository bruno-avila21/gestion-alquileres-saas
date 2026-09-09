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
    /// <summary>Pending charges (RentCharge/ManualDebit, Status=Pending) of a contract, oldest first — for payment allocation.</summary>
    Task<IReadOnlyList<Transaction>> GetPendingChargesAsync(Guid contractId, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit, CancellationToken ct);
    Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken ct);
    Task AddAsync(Transaction transaction, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
