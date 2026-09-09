using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Domain.Reports;

namespace GestionAlquileres.Tests.Phase13.Application;

// ---------------------------------------------------------------------------
// Hand-rolled fakes/stubs (mismo estilo que Phase2/Application/*Tests.cs — sin librería de mocking).
// ---------------------------------------------------------------------------

internal sealed class FakeCurrentTenant : ICurrentTenant
{
    public Guid OrganizationId { get; set; }
}

internal sealed class FakeTransactionRepository : ITransactionRepository
{
    public List<Transaction> All = new();

    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct) =>
        // Simula el filtro global de tenant: sólo "ve" las transacciones de la organización que
        // el test le carga en la lista — el aislamiento real se prueba a nivel HTTP.
        Task.FromResult(All.FirstOrDefault(t => t.Id == id));

    public Task<(IReadOnlyList<Transaction> Items, int Total, decimal NetBalance)> GetPagedAsync(
        TransactionType? type, string? search, int page, int pageSize, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Transaction>> GetByContractAsync(Guid contractId, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<OwnerCollectedRow>> GetCollectedByOwnerAsync(
        Guid ownerId, DateOnly periodFrom, DateOnly periodTo, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Transaction>> GetPendingChargesAsync(Guid contractId, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Transaction>> GetRecentAsync(int limit, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken ct) =>
        throw new NotImplementedException();

    public Task AddAsync(Transaction transaction, CancellationToken ct)
    {
        All.Add(transaction);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask; // las entidades son las mismas instancias en memoria
}

internal sealed class FakeContractRepository : IContractRepository
{
    public List<Contract> All = new();

    public Task<Contract?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(All.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Contract>> ListAsync(Guid? appTenantId, Guid? propertyId, ContractStatus? status, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<bool> HasActiveOverlapAsync(Guid propertyId, DateOnly startDate, DateOnly endDate, Guid? excludeContractId, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task AddAsync(Contract contract, CancellationToken ct) => throw new NotImplementedException();
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

    public Task<(int ActiveCount, decimal MonthlyRevenue, int ExpiringCount)> GetDashboardStatsAsync(
        DateOnly today, DateOnly until, CancellationToken ct) => throw new NotImplementedException();

    public Task<Contract?> GetByIdRawAsync(Guid id, Guid organizationId, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<Contract>> ListActiveRawAsync(CancellationToken ct) => throw new NotImplementedException();
    public Task<IReadOnlyList<Contract>> GetExpiringRawAsync(int daysAhead, CancellationToken ct) => throw new NotImplementedException();
}

internal sealed class FakeOrganizationRepository : IOrganizationRepository
{
    public Organization? Org;
    public int IncrementCalls;

    public Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(Org is not null && Org.Id == id ? Org : null);

    public Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct) => throw new NotImplementedException();
    public Task<bool> SlugExistsAsync(string slug, CancellationToken ct) => throw new NotImplementedException();
    public Task AddAsync(Organization org, CancellationToken ct) => throw new NotImplementedException();
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;

    /// <summary>Incremento atómico simulado: en el test corre en un solo hilo, así que alcanza con
    /// mutar el contador en memoria — lo que se prueba acá es la regla de negocio del handler
    /// (mismo número en el segundo pedido, números consecutivos entre transacciones distintas), no
    /// la serialización real de Postgres (eso lo garantiza OrganizationRepository.IncrementReceiptSequenceAsync).</summary>
    public Task<long> IncrementReceiptSequenceAsync(Guid organizationId, CancellationToken ct)
    {
        IncrementCalls++;
        Org!.ReceiptSequence++;
        return Task.FromResult(Org.ReceiptSequence);
    }
}

internal sealed class FakeOwnerRepository : IOwnerRepository
{
    public List<Owner> All = new();

    public Task<Owner?> GetByIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(All.FirstOrDefault(o => o.Id == id));

    public Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken ct) => throw new NotImplementedException();
    public Task AddAsync(Owner owner, CancellationToken ct) => throw new NotImplementedException();
    public Task<bool> ExistsAsync(Guid id, CancellationToken ct) => Task.FromResult(All.Any(o => o.Id == id));
    public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakeStorageService : IStorageService
{
    public Task<string> UploadAsync(Stream content, string fileName, string mimeType, CancellationToken ct) =>
        Task.FromResult("fake-key" + Path.GetExtension(fileName));

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken ct) =>
        Task.FromResult<Stream>(new MemoryStream(new byte[] { 1, 2, 3 }));

    public Task DeleteAsync(string storageKey, CancellationToken ct) => Task.CompletedTask;
}

internal sealed class FakePdfReportGenerator : IPdfReportGenerator
{
    public List<ReceiptReport> Receipts = new();
    public List<OwnerSettlementReport> Settlements = new();

    public byte[] RenderReceipt(ReceiptReport report)
    {
        Receipts.Add(report);
        return new byte[] { 0x25, 0x50, 0x44, 0x46 }; // "%PDF"
    }

    public byte[] RenderOwnerSettlement(OwnerSettlementReport report)
    {
        Settlements.Add(report);
        return new byte[] { 0x25, 0x50, 0x44, 0x46 };
    }
}
