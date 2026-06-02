using GestionAlquileres.Application.Common.DTOs;

namespace GestionAlquileres.Application.Features.Auth.DTOs;

/// <summary>Raw refresh token (to hand to the client) plus its expiry. The raw value is never persisted.</summary>
public record RefreshTokenResult(string RawToken, DateTimeOffset ExpiresAt);

/// <summary>Result of a successful refresh: a fresh access token (Auth) and a rotated refresh token.</summary>
public record RefreshAccessTokenResult(AuthResponseDto Auth, string RawToken, DateTimeOffset ExpiresAt);
