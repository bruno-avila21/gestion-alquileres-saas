using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface ISiteSettingsRepository
{
    /// <summary>La configuración de la organización actual, o null si nunca la tocaron.</summary>
    Task<SiteSettings?> GetAsync(CancellationToken ct);
    Task AddAsync(SiteSettings settings, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
