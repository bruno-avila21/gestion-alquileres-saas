using GestionAlquileres.Application.Features.AppTenants.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public record UpdateAppTenantCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Dni,
    string? Email,
    string? Phone,
    bool IsActive)
    : IRequest<AppTenantDto>;
