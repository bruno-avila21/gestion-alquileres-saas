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
    /// <summary>Paleta del panel. Nunca null hacia afuera: se resuelve a la predeterminada.</summary>
    string PanelPalette,
    bool HasLogo,
    string Plan)
{
    public static OrganizationDto From(Organization org) => new(
        org.Id, org.Name, org.LegalName, org.TaxId, org.Address, org.Phone, org.Email,
        org.BrandColor,
        PanelPalette: PanelPalettes.IsValid(org.PanelPalette) ? org.PanelPalette! : PanelPalettes.Default,
        HasLogo: !string.IsNullOrWhiteSpace(org.LogoStorageKey), org.Plan);
}
