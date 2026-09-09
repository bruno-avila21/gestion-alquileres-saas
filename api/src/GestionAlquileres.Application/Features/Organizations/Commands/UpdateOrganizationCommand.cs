using GestionAlquileres.Application.Features.Organizations.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Commands;

/// <summary>OrganizationId no viaja en el comando: el handler siempre usa ICurrentTenant.</summary>
public record UpdateOrganizationCommand(
    string Name,
    string? LegalName,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Email,
    string? BrandColor) : IRequest<OrganizationDto>;
