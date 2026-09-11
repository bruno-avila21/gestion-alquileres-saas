using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Common.Settings;
using Microsoft.Extensions.Options;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class RegisterOrgCommandHandler : IRequestHandler<RegisterOrgCommand, AuthResponseDto>
{
    private readonly IOrganizationRepository _orgRepo;
    private readonly IUserRepository _userRepo;
    private readonly IJwtService _jwt;
    private readonly RegistrationSettings _registration;

    public RegisterOrgCommandHandler(
        IOrganizationRepository orgRepo,
        IUserRepository userRepo,
        IJwtService jwt,
        IOptions<RegistrationSettings> registration)
    {
        _orgRepo = orgRepo;
        _userRepo = userRepo;
        _jwt = jwt;
        _registration = registration.Value;
    }

    public async Task<AuthResponseDto> Handle(RegisterOrgCommand request, CancellationToken ct)
    {
        EnsureRegistrationAllowed(request.InviteCode);

        var slug = request.Slug.ToLowerInvariant();
        if (await _orgRepo.SlugExistsAsync(slug, ct))
            throw new BusinessException($"Organization slug '{slug}' is already taken.");

        var org = new Organization
        {
            Name = request.OrganizationName,
            Slug = slug,
            Plan = "free",
            IsActive = true
        };
        await _orgRepo.AddAsync(org, ct);

        var user = new User
        {
            OrganizationId = org.Id,
            Email = request.AdminEmail.ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.AdminPassword),
            FirstName = request.AdminFirstName,
            LastName = request.AdminLastName,
            Role = UserRole.Admin,
            IsActive = true
        };
        await _userRepo.AddAsync(user, ct);
        await _orgRepo.SaveChangesAsync(ct); // single DbContext → single transaction

        return new AuthResponseDto(
            _jwt.GenerateToken(user),
            user.Id,
            user.Email,
            user.Role.ToString(),
            org.Id,
            org.Slug);
    }

    /// <summary>
    /// Aplica el modo de alta configurado. El endpoint es anónimo, así que sin esto cualquiera
    /// creaba organizaciones ilimitadas y ocupaba slugs de marcas reales de forma irrecuperable.
    /// </summary>
    private void EnsureRegistrationAllowed(string? providedCode)
    {
        switch (_registration.Mode)
        {
            case RegistrationMode.Disabled:
                throw new BusinessException(
                    "El alta de organizaciones está deshabilitada. Contactá al administrador de la plataforma.");

            case RegistrationMode.InviteCode:
                var expected = _registration.InviteCode;
                if (string.IsNullOrWhiteSpace(expected))
                    throw new BusinessException(
                        "El alta de organizaciones no está configurada correctamente. Contactá al administrador de la plataforma.");

                // Comparación en tiempo constante: el código es un secreto compartido y compararlo
                // con == filtra por dónde difiere.
                var provided = System.Text.Encoding.UTF8.GetBytes(providedCode ?? string.Empty);
                var reference = System.Text.Encoding.UTF8.GetBytes(expected);
                if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(provided, reference))
                    throw new BusinessException("El código de invitación no es válido.");
                break;

            case RegistrationMode.Open:
            default:
                break;
        }
    }
}
