using GestionAlquileres.Application.Features.AppTenants.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Queries;

public record GetAppTenantByIdQuery(Guid Id) : IRequest<AppTenantDto?>;
