using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtService _jwt;

    // A valid bcrypt hash of a throwaway value, computed once. When the org/user doesn't exist we
    // still run a Verify against this so the response time matches the found-user path, closing the
    // timing side-channel that let an attacker enumerate valid (org, email) pairs (audit M2).
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("timing-equalizer");

    public LoginCommandHandler(IOrganizationRepository orgRepo, IUserRepository userRepo, IJwtService jwt)
    {
        _orgRepo = orgRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken ct)
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

        if (!user.IsActive || user.Role == UserRole.Tenant)
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
