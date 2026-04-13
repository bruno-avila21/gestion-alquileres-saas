using GestionAlquileres.Application.Common.DTOs;
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

    public RegisterOrgCommandHandler(
        IOrganizationRepository orgRepo,
        IUserRepository userRepo,
        IJwtService jwt)
    {
        _orgRepo = orgRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(RegisterOrgCommand request, CancellationToken ct)
    {
        var slug = request.Slug.ToLowerInvariant();
        if (await _orgRepo.SlugExistsAsync(slug, ct))
            throw new InvalidOperationException($"Organization slug '{slug}' is already taken.");

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
}
