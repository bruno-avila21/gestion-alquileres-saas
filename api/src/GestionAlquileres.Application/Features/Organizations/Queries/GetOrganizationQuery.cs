using GestionAlquileres.Application.Features.Organizations.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Queries;

/// <summary>La organización del tenant actual (OrganizationId sale del JWT, nunca de la ruta).</summary>
public record GetOrganizationQuery : IRequest<OrganizationDto>;
