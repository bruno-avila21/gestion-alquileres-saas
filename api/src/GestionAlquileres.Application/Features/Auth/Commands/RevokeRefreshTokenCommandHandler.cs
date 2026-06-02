using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class RevokeRefreshTokenCommandHandler : IRequestHandler<RevokeRefreshTokenCommand>
{
    private readonly IRefreshTokenRepository _tokens;
    private readonly IRefreshTokenService _service;

    public RevokeRefreshTokenCommandHandler(IRefreshTokenRepository tokens, IRefreshTokenService service)
    {
        _tokens = tokens;
        _service = service;
    }

    public async Task Handle(RevokeRefreshTokenCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RawToken)) return;

        var stored = await _tokens.GetByHashAsync(_service.Hash(request.RawToken), ct);
        if (stored is null || stored.RevokedAt is not null) return;

        stored.RevokedAt = DateTimeOffset.UtcNow;
        await _tokens.SaveChangesAsync(ct);
    }
}
