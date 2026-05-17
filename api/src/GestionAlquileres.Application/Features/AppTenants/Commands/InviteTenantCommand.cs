using GestionAlquileres.Application.Features.AppTenants.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public record InviteTenantCommand(Guid AppTenantId) : IRequest<InviteTenantResult>;
