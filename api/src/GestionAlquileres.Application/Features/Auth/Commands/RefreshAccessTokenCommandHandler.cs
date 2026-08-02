using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Auth.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class RefreshAccessTokenCommandHandler
    : IRequestHandler<RefreshAccessTokenCommand, RefreshAccessTokenResult>
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IOrganizationRepository _orgs;
    private readonly IRefreshTokenService _service;
    private readonly IJwtService _jwt;
    private readonly ILogger<RefreshAccessTokenCommandHandler> _logger;

    public RefreshAccessTokenCommandHandler(
        IRefreshTokenRepository tokens,
        IUserRepository users,
        IOrganizationRepository orgs,
        IRefreshTokenService service,
        IJwtService jwt,
        ILogger<RefreshAccessTokenCommandHandler> logger)
    {
        _tokens = tokens;
        _users = users;
        _orgs = orgs;
        _service = service;
        _jwt = jwt;
        _logger = logger;
    }

    public async Task<RefreshAccessTokenResult> Handle(RefreshAccessTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RawToken))
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var hash = _service.Hash(request.RawToken);
        var stored = await _tokens.GetByHashAsync(hash, ct);

        if (stored is null)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        // Reuse detection: a token that was already rotated/revoked is being presented again.
        // Treat as compromise and revoke the user's whole refresh-token family.
        if (stored.RevokedAt is not null)
        {
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}; revoking all tokens.", stored.UserId);
            await _tokens.RevokeAllForUserAsync(stored.UserId, ct);
            await _tokens.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        if (DateTimeOffset.UtcNow >= stored.ExpiresAt)
            throw new UnauthorizedAccessException("Expired refresh token.");

        var user = await _users.GetByIdAcrossOrgsAsync(stored.UserId, ct);
        if (user is null || !user.IsActive)
            throw new UnauthorizedAccessException("Invalid refresh token.");

        var org = await _orgs.GetByIdAsync(stored.OrganizationId, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        // Rotate: mint a new token, revoke the old one and link them for audit/reuse detection.
        var newRaw = _service.GenerateRawToken();
        var newHash = _service.Hash(newRaw);
        var expiresAt = DateTimeOffset.UtcNow.Add(_service.Lifetime);

        await _tokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            OrganizationId = org.Id,
            TokenHash = newHash,
            ExpiresAt = expiresAt,
        }, ct);

        stored.RevokedAt = DateTimeOffset.UtcNow;
        stored.ReplacedByTokenHash = newHash;
        await _tokens.SaveChangesAsync(ct);

        var auth = new AuthResponseDto(
            _jwt.GenerateToken(user),
            user.Id,
            user.Email,
            user.Role.ToString(),
            org.Id,
            org.Slug,
            user.MustChangePassword);

        return new RefreshAccessTokenResult(auth, newRaw, expiresAt);
    }
}
