using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>By-id lookup that bypasses the tenant filter — for flows with no tenant in scope (e.g. token refresh).</summary>
    Task<User?> GetByIdAcrossOrgsAsync(Guid id, CancellationToken ct);

    Task<User?> GetByEmailAsync(Guid organizationId, string email, CancellationToken ct);
    Task<User?> GetByEmailAcrossOrgsAsync(string email, CancellationToken ct);

    /// <summary>Usuarios activos de una organización con un rol dado (p. ej. admins a notificar). No hay tenant en scope garantizado en el punto de llamada; el `organizationId` se filtra explícito.</summary>
    Task<IReadOnlyList<User>> GetActiveByRoleAsync(Guid organizationId, UserRole role, CancellationToken ct);

    Task AddAsync(User user, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
