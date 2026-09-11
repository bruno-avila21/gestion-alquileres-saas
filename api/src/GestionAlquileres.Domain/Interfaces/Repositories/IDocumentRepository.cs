using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IDocumentRepository
{
    Task<IReadOnlyList<Document>> GetByContractAsync(Guid contractId, CancellationToken ct);
    Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct);
    /// <summary>One page of the org's documents (optionally filtered by file name), plus the total count.</summary>
    Task<(IReadOnlyList<Document> Items, int Total)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct);
    Task<Document?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Document?> GetByIdRawAsync(Guid id, Guid organizationId, CancellationToken ct);
    Task AddAsync(Document document, CancellationToken ct);
    Task RemoveAsync(Document document, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
