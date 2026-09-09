using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Application.Features.SiteSettings.DTOs;

/// <summary>
/// El aspecto y los textos del sitio público. Los campos van en null cuando la inmobiliaria
/// no los tocó: es el frontend el que decide qué mostrar por defecto, no la base. Así el
/// diseño por defecto puede cambiar sin tener que migrar filas.
/// </summary>
public record SiteSettingsDto(
    string? AccentColor,
    string? FontPairing,
    string? HeroTitle,
    string? HeroSubtitle,
    string? AboutText,
    string? FooterTagline)
{
    public static SiteSettingsDto From(Domain.Entities.SiteSettings? s) => new(
        s?.AccentColor,
        SiteFontPairings.IsValid(s?.FontPairing) ? s!.FontPairing : null,
        s?.HeroTitle,
        s?.HeroSubtitle,
        s?.AboutText,
        s?.FooterTagline);

    /// <summary>Lo que ve una inmobiliaria que nunca entró al editor.</summary>
    public static SiteSettingsDto Empty => new(null, null, null, null, null, null);
}
