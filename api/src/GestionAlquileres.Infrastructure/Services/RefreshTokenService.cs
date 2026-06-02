using System.Security.Cryptography;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.Infrastructure.Services;

/// <summary>
/// Generates high-entropy refresh tokens and hashes them for storage. The raw token is shown to the
/// client exactly once; only its SHA-256 hash is ever persisted, so a DB leak yields no usable token.
/// </summary>
public class RefreshTokenService : IRefreshTokenService
{
    private readonly JwtSettings _settings;

    public RefreshTokenService(IOptions<JwtSettings> settings) => _settings = settings.Value;

    public TimeSpan Lifetime => TimeSpan.FromDays(_settings.RefreshTokenDays > 0 ? _settings.RefreshTokenDays : 14);

    public string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32); // 256 bits
        return Base64UrlEncode(bytes);
    }

    public string Hash(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
