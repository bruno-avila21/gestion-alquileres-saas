using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.AppTenants.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class InviteTenantCommandHandler : IRequestHandler<InviteTenantCommand, InviteTenantResult>
{
    private readonly IAppTenantRepository _tenantRepo;
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokens;

    public InviteTenantCommandHandler(
        IAppTenantRepository tenantRepo,
        IUserRepository userRepo,
        IRefreshTokenRepository refreshTokens)
    {
        _tenantRepo = tenantRepo;
        _userRepo = userRepo;
        _refreshTokens = refreshTokens;
    }

    public async Task<InviteTenantResult> Handle(InviteTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(request.AppTenantId, ct)
            ?? throw new BusinessException($"Inquilino {request.AppTenantId} not found.");

        if (string.IsNullOrWhiteSpace(tenant.Email))
            throw new BusinessException("El inquilino debe tener un email para recibir acceso al portal.");

        var tempPassword = GenerateTempPassword();
        var hash = BCrypt.Net.BCrypt.HashPassword(tempPassword);

        if (tenant.UserId is { } existingUserId)
        {
            // Re-invitar en vez de rechazar. Antes esto lanzaba "ya tiene acceso al portal", con lo
            // cual una contraseña temporal filtrada o perdida no se podía rotar por ninguna vía:
            // había que tocar la base a mano. Es además el único camino para devolverle el acceso a
            // un inquilino dado de baja, porque reactivarlo desde la edición no reactiva su usuario.
            var existing = await _userRepo.GetByIdAsync(existingUserId, ct)
                ?? throw new BusinessException("El usuario vinculado a este inquilino no existe.");

            existing.PasswordHash = hash;
            existing.MustChangePassword = true;
            existing.PasswordChangedAt = DateTimeOffset.UtcNow;
            existing.IsActive = true;
            existing.Email = tenant.Email;

            // La credencial anterior deja de servir en todos lados, no sólo para nuevos ingresos.
            await _refreshTokens.RevokeAllForUserAsync(existingUserId, ct);

            await _tenantRepo.SaveChangesAsync(ct);
            return new InviteTenantResult(CreateAppTenantCommandHandler.ToDto(tenant), tempPassword);
        }

        var user = new User
        {
            OrganizationId = tenant.OrganizationId,
            Email = tenant.Email,
            PasswordHash = hash,
            FirstName = tenant.FirstName,
            LastName = tenant.LastName,
            Role = UserRole.Tenant,
            IsActive = true,
            // La contraseña la generó el sistema y viaja por WhatsApp o email: queda en el historial
            // de esa conversación, así que no puede ser la credencial definitiva.
            MustChangePassword = true,
        };

        await _userRepo.AddAsync(user, ct);
        tenant.UserId = user.Id;
        await _tenantRepo.SaveChangesAsync(ct);

        return new InviteTenantResult(CreateAppTenantCommandHandler.ToDto(tenant), tempPassword);
    }

    private static string GenerateTempPassword()
    {
        const string chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789!@#$";
        // Use rejection sampling (RandomNumberGenerator.GetInt32) rather than `b % chars.Length`,
        // which biases toward the first (256 % 57) characters (audit B4).
        var result = new char[12];
        for (var i = 0; i < result.Length; i++)
            result[i] = chars[System.Security.Cryptography.RandomNumberGenerator.GetInt32(chars.Length)];
        return new string(result);
    }
}
