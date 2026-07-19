using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly AppDbContext _db;
    public DocumentRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<Document>> GetByContractAsync(Guid contractId, CancellationToken ct) =>
        await _db.Documents
            .Where(d => d.ContractId == contractId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken ct) =>
        await _db.Documents
            .OrderByDescending(d => d.CreatedAt)
            .Take(500)
            .ToListAsync(ct);

    public async Task<(IReadOnlyList<Document> Items, int Total)> GetPagedAsync(
        string? search, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.Documents.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(d => d.FileName.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return (items, total);
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Documents.FirstOrDefaultAsync(d => d.Id == id, ct);

    // Bypasses tenant filter — safe only when orgId comes from a server-signed token.
    public Task<Document?> GetByIdRawAsync(Guid id, Guid organizationId, CancellationToken ct) =>
        _db.Documents
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id && d.OrganizationId == organizationId, ct);

    public async Task AddAsync(Document document, CancellationToken ct) =>
        await _db.Documents.AddAsync(document, ct);

    public Task RemoveAsync(Document document, CancellationToken ct)
    {
        _db.Documents.Remove(document);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
