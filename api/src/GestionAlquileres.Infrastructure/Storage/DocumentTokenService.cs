using System.Security.Cryptography;
using System.Text;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace GestionAlquileres.Infrastructure.Storage;

public class DocumentTokenService : IDocumentTokenService
{
    private readonly byte[] _key;
    public int ExpiryMinutes => 5;

    public DocumentTokenService(IConfiguration configuration)
    {
        var secret = configuration["DocumentToken:Secret"]
            ?? throw new InvalidOperationException("DocumentToken:Secret missing in configuration.");
        _key = Encoding.UTF8.GetBytes(secret);
    }

    public string Generate(Guid documentId, Guid organizationId)
    {
        var exp = DateTimeOffset.UtcNow.AddMinutes(ExpiryMinutes).ToUnixTimeSeconds();
        var payload = Encoding.UTF8.GetBytes($"{documentId}:{organizationId}:{exp}");
        using var hmac = new HMACSHA256(_key);
        var sig = hmac.ComputeHash(payload);
        return Base64UrlEncode(payload) + "." + Base64UrlEncode(sig);
    }

    public (Guid documentId, Guid organizationId)? Validate(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var dot = token.LastIndexOf('.');
        if (dot < 0) return null;

        byte[] payload, sig;
        try
        {
            payload = Base64UrlDecode(token[..dot]);
            sig     = Base64UrlDecode(token[(dot + 1)..]);
        }
        catch { return null; }

        using var hmac = new HMACSHA256(_key);
        var expected = hmac.ComputeHash(payload);
        if (!CryptographicOperations.FixedTimeEquals(sig, expected)) return null;

        var parts = Encoding.UTF8.GetString(payload).Split(':');
        if (parts.Length != 3) return null;
        if (!Guid.TryParse(parts[0], out var docId)) return null;
        if (!Guid.TryParse(parts[1], out var orgId)) return null;
        if (!long.TryParse(parts[2], out var exp)) return null;
        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > exp) return null;

        return (docId, orgId);
    }

    private static string Base64UrlEncode(byte[] data) =>
        Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(s);
    }
}
