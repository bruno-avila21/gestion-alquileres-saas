using GestionAlquileres.Application.Features.Organizations.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Queries;

/// <summary>Null cuando la organización no tiene logo cargado — el controller devuelve 404.</summary>
public record GetOrganizationLogoQuery : IRequest<OrganizationLogoDto?>;
