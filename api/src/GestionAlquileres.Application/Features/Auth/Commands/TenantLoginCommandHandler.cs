using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class TenantLoginCommandHandler : IRequestHandler<TenantLoginCommand, AuthResponseDto>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtService _jwt;

    // See LoginCommandHandler: equalize response time on the not-found paths to prevent account
    // enumeration via a timing side-channel (audit M2).
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("timing-equalizer");

    public TenantLoginCommandHandler(IOrganizationRepository orgRepo, IUserRepository userRepo, IJwtService jwt)
    {
        _orgRepo = orgRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(TenantLoginCommand request, CancellationToken ct)
    {
        var org = await _orgRepo.GetBySlugAsync(request.OrganizationSlug.ToLowerInvariant(), ct);
        if (org is null)
        {
            BCrypt.Net.BCrypt.Verify(request.Password, DummyHash);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var user = await _userRepo.GetByEmailAsync(org.Id, request.Email.ToLowerInvariant(), ct);
        if (user is null)
        {
            BCrypt.Net.BCrypt.Verify(request.Password, DummyHash);
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        // CRITICAL: this endpoint is Tenant-only. Admin/Staff MUST use /auth/login.
        if (!user.IsActive || user.Role != UserRole.Tenant)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        // La organización puede estar suspendida (morosa, dada de baja, comprometida). Se verifica
        // DESPUÉS de validar la contraseña, a propósito: hacerlo antes convertiría la respuesta en
        // un oráculo que revela qué inmobiliarias existen y cuáles están suspendidas, sin necesidad
        // de credenciales válidas.
        if (!org.IsActive)
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthResponseDto(
            _jwt.GenerateToken(user),
            user.Id,
            user.Email,
            user.Role.ToString(),
            org.Id,
            org.Slug,
            user.MustChangePassword);
    }
}
