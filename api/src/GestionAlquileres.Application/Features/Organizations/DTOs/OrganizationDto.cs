using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Application.Features.Organizations.DTOs;

/// <summary>
/// No expone <c>LogoStorageKey</c>: el frontend pide el logo por GET /organization/logo, nunca por
/// una URL directa al storage.
/// </summary>
public record OrganizationDto(
    Guid Id,
    string Name,
    string? LegalName,
    string? TaxId,
    string? Address,
    string? Phone,
    string? Email,
    string? BrandColor,
    bool HasLogo,
    string Plan)
{
    public static OrganizationDto From(Organization org) => new(
        org.Id, org.Name, org.LegalName, org.TaxId, org.Address, org.Phone, org.Email,
        org.BrandColor, HasLogo: !string.IsNullOrWhiteSpace(org.LogoStorageKey), org.Plan);
}
