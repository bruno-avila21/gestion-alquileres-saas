using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, AuthResponseDto>
{
    private readonly IUserRepository _users;
    private readonly IOrganizationRepository _orgs;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtService _jwt;

    public ChangePasswordCommandHandler(
        IUserRepository users,
        IOrganizationRepository orgs,
        IRefreshTokenRepository refreshTokens,
        IJwtService jwt)
    {
        _users = users;
        _orgs = orgs;
        _refreshTokens = refreshTokens;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        // Sin filtro de tenant: el id sale del JWT, así que ya está acotado al usuario que pide.
        var user = await _users.GetByIdAcrossOrgsAsync(request.UserId, ct)
            ?? throw new UnauthorizedAccessException("Usuario no encontrado.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("La cuenta está desactivada.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BusinessException("La contraseña actual no es correcta.");

        var org = await _orgs.GetByIdAsync(user.OrganizationId, ct)
            ?? throw new UnauthorizedAccessException("Organización no encontrada.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.PasswordChangedAt = DateTimeOffset.UtcNow;

        // Cambiar la contraseña termina TODAS las sesiones abiertas: es la reacción esperable si el
        // motivo del cambio es que la credencial se filtró. El controller emite un par nuevo para
        // la sesión que hizo el cambio, así que quien lo pidió no se queda afuera.
        await _refreshTokens.RevokeAllForUserAsync(user.Id, ct);

        await _users.SaveChangesAsync(ct);

        return new AuthResponseDto(
            _jwt.GenerateToken(user),
            user.Id,
            user.Email,
            user.Role.ToString(),
            user.OrganizationId,
            org.Slug,
            MustChangePassword: false);
    }
}
