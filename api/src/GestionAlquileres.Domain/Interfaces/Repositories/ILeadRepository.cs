using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface ILeadRepository
{
    /// <summary>Lead with Listing/Property and Notes (desc by CreatedAt) loaded.</summary>
    Task<Lead?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Same as <see cref="GetByIdAsync"/> but without notes — enough for update/status/delete flows.</summary>
    Task<Lead?> GetForEditAsync(Guid id, CancellationToken ct);

    Task<(IReadOnlyList<Lead> Items, int Total)> GetPagedAsync(
        LeadStatus? status, string? search, int page, int pageSize, CancellationToken ct);

    /// <summary>Total leads and a count per <see cref="LeadStatus"/> for the kanban headers.</summary>
    Task<(int Total, IReadOnlyDictionary<LeadStatus, int> ByStatus)> GetSummaryAsync(CancellationToken ct);

    Task AddAsync(Lead lead, CancellationToken ct);
    Task AddNoteAsync(LeadNote note, CancellationToken ct);
    void Remove(Lead lead);
    Task SaveChangesAsync(CancellationToken ct);
}
