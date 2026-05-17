using MediatR;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public record DeleteAppTenantCommand(Guid Id) : IRequest;
