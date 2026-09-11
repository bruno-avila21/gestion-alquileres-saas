namespace GestionAlquileres.API.Contracts;

public record UpdateSiteSettingsRequest(
    string? AccentColor,
    string? FontPairing,
    string? HeroTitle,
    string? HeroSubtitle,
    string? AboutText,
    string? FooterTagline);
