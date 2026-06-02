namespace GestionAlquileres.Domain.Interfaces.Services;

public interface IRefreshTokenService
{
    /// <summary>Generates a new high-entropy raw token (URL-safe). Hand this to the client; never store it.</summary>
    string GenerateRawToken();

    /// <summary>Hex-encoded SHA-256 of a raw token. Deterministic — used both to store and to look up.</summary>
    string Hash(string rawToken);

    /// <summary>How long an issued refresh token stays valid.</summary>
    TimeSpan Lifetime { get; }
}
