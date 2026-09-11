using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IListingRepository
{
    /// <summary>Listing with its Property loaded.</summary>
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<Listing>> GetAllAsync(Guid? propertyId, CancellationToken ct);

    /// <summary>
    /// Every published listing of the current tenant with its Property, for the public search.
    /// Filtering and facets are computed by the caller: a public site holds tens to a few hundred
    /// listings, and facet counts need the whole set anyway.
    /// </summary>
    Task<IReadOnlyList<Listing>> GetPublishedAsync(CancellationToken ct);

    Task<bool> ExistsPublishedForPropertyAsync(Guid propertyId, CancellationToken ct);
    Task AddAsync(Listing listing, CancellationToken ct);
    void Remove(Listing listing);
    Task SaveChangesAsync(CancellationToken ct);
}
