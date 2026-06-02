using GestionAlquileres.Application.Features.Auth.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class IssueRefreshTokenCommandHandler : IRequestHandler<IssueRefreshTokenCommand, RefreshTokenResult>
{
    private readonly IRefreshTokenRepository _repo;
    private readonly IRefreshTokenService _service;

    public IssueRefreshTokenCommandHandler(IRefreshTokenRepository repo, IRefreshTokenService service)
    {
        _repo = repo;
        _service = service;
    }

    public async Task<RefreshTokenResult> Handle(IssueRefreshTokenCommand request, CancellationToken ct)
    {
        var raw = _service.GenerateRawToken();
        var expiresAt = DateTimeOffset.UtcNow.Add(_service.Lifetime);

        await _repo.AddAsync(new RefreshToken
        {
            UserId = request.UserId,
            OrganizationId = request.OrganizationId,
            TokenHash = _service.Hash(raw),
            ExpiresAt = expiresAt,
        }, ct);
        await _repo.SaveChangesAsync(ct);

        return new RefreshTokenResult(raw, expiresAt);
    }
}
