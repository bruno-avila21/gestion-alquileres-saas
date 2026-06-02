namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// A long-lived credential that lets a client mint a fresh access token without re-entering
/// credentials. Only the SHA-256 HASH of the raw token is stored — a DB leak never exposes a
/// usable token. NOT an ITenantEntity: the /refresh endpoint runs without a tenant in scope, so
/// this table has no global query filter; the owning organization is carried explicitly.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }

    /// <summary>Hex-encoded SHA-256 of the raw token. Unique. The raw token is never persisted.</summary>
    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>Hash of the token that superseded this one on rotation (audit / reuse detection).</summary>
    public string? ReplacedByTokenHash { get; set; }

    public bool IsActive => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}
