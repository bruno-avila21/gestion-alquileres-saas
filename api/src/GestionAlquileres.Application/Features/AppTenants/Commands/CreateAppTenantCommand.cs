using GestionAlquileres.Application.Features.AppTenants.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public record CreateAppTenantCommand(
    Guid OrganizationId,
    string FirstName,
    string LastName,
    string Dni,
    string? Email,
    string? Phone)
    : IRequest<AppTenantDto>;
