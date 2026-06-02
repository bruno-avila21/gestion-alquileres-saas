using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

/// <summary>Revokes a single refresh token (logout). No-op if the token is unknown or already revoked.</summary>
public record RevokeRefreshTokenCommand(string? RawToken) : IRequest;
