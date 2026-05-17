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

    public InviteTenantCommandHandler(
        IAppTenantRepository tenantRepo,
        IUserRepository userRepo)
    {
        _tenantRepo = tenantRepo;
        _userRepo = userRepo;
    }

    public async Task<InviteTenantResult> Handle(InviteTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(request.AppTenantId, ct)
            ?? throw new BusinessException($"Inquilino {request.AppTenantId} not found.");

        if (tenant.UserId.HasValue)
            throw new BusinessException("Este inquilino ya tiene acceso al portal.");

        if (string.IsNullOrWhiteSpace(tenant.Email))
            throw new BusinessException("El inquilino debe tener un email para recibir acceso al portal.");

        var tempPassword = GenerateTempPassword();

        var user = new User
        {
            OrganizationId = tenant.OrganizationId,
            Email = tenant.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            FirstName = tenant.FirstName,
            LastName = tenant.LastName,
            Role = UserRole.Tenant,
            IsActive = true,
        };

        await _userRepo.AddAsync(user, ct);
        tenant.UserId = user.Id;
        await _tenantRepo.SaveChangesAsync(ct);

        return new InviteTenantResult(CreateAppTenantCommandHandler.ToDto(tenant), tempPassword);
    }

    private static string GenerateTempPassword()
    {
        const string chars = "abcdefghjkmnpqrstuvwxyzABCDEFGHJKMNPQRSTUVWXYZ23456789!@#$";
        var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var bytes = new byte[12];
        rng.GetBytes(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}
