using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository
{
    /// <summary>Looks up a token by its hash. No tenant filter — refresh runs without a tenant in scope.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct);

    Task AddAsync(RefreshToken token, CancellationToken ct);

    /// <summary>Revokes every still-active token for a user (used on reuse detection / global logout).</summary>
    Task RevokeAllForUserAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Permanently deletes tokens whose <c>ExpiresAt</c> is before <paramref name="cutoff"/>.
    /// Expired tokens can no longer be used or replayed, so purging them keeps the table bounded.
    /// Returns the number of rows removed.
    /// </summary>
    Task<int> DeleteExpiredAsync(DateTimeOffset cutoff, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
