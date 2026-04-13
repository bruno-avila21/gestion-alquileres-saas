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

    public LoginCommandHandler(IOrganizationRepository orgRepo, IUserRepository userRepo, IJwtService jwt)
    {
        _orgRepo = orgRepo;
        _userRepo = userRepo;
        _jwt = jwt;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken ct)
    {
        var org = await _orgRepo.GetBySlugAsync(request.OrganizationSlug.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        var user = await _userRepo.GetByEmailAsync(org.Id, request.Email.ToLowerInvariant(), ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive || user.Role == UserRole.Tenant)
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        return new AuthResponseDto(
            _jwt.GenerateToken(user),
            user.Id,
            user.Email,
            user.Role.ToString(),
            org.Id,
            org.Slug);
    }
}
