using FluentValidation;
using GestionAlquileres.Application.Features.SiteSettings.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.SiteSettings.Commands;

/// <summary>OrganizationId no viaja en el comando: el handler siempre usa ICurrentTenant.</summary>
public record UpdateSiteSettingsCommand(
    string? AccentColor,
    string? FontPairing,
    string? HeroTitle,
    string? HeroSubtitle,
    string? AboutText,
    string? FooterTagline) : IRequest<SiteSettingsDto>;

public class UpdateSiteSettingsCommandValidator : AbstractValidator<UpdateSiteSettingsCommand>
{
    private const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";

    public UpdateSiteSettingsCommandValidator()
    {
        RuleFor(x => x.AccentColor).Matches(HexColorPattern)
            .WithMessage("El color de acento debe tener el formato #RRGGBB.")
            .When(x => !string.IsNullOrWhiteSpace(x.AccentColor));

        RuleFor(x => x.FontPairing)
            .Must(SiteFontPairings.IsValid)
            .WithMessage($"La tipografía debe ser una de: {string.Join(", ", SiteFontPairings.All)}.")
            .When(x => !string.IsNullOrWhiteSpace(x.FontPairing));

        // Los topes coinciden con las columnas: sin esto un texto más largo llegaba a Postgres
        // y volvía como 500 en vez de un 400 que diga qué campo se pasó.
        RuleFor(x => x.HeroTitle).MaximumLength(160);
        RuleFor(x => x.HeroSubtitle).MaximumLength(400);
        RuleFor(x => x.AboutText).MaximumLength(2000);
        RuleFor(x => x.FooterTagline).MaximumLength(200);
    }
}

public class UpdateSiteSettingsCommandHandler
    : IRequestHandler<UpdateSiteSettingsCommand, SiteSettingsDto>
{
    private readonly ISiteSettingsRepository _repo;
    private readonly ICurrentTenant _currentTenant;

    public UpdateSiteSettingsCommandHandler(ISiteSettingsRepository repo, ICurrentTenant currentTenant)
    {
        _repo = repo;
        _currentTenant = currentTenant;
    }

    public async Task<SiteSettingsDto> Handle(UpdateSiteSettingsCommand request, CancellationToken ct)
    {
        var settings = await _repo.GetAsync(ct);

        // La fila se crea recién la primera vez que alguien guarda. Antes de eso el sitio usa
        // los valores por defecto del diseño, así que no hace falta sembrar una fila por
        // organización al darla de alta.
        if (settings is null)
        {
            settings = new Domain.Entities.SiteSettings { OrganizationId = _currentTenant.OrganizationId };
            await _repo.AddAsync(settings, ct);
        }

        // Vacío se guarda como null, no como cadena vacía: "borré el título" y "nunca lo
        // escribí" tienen que significar lo mismo para el sitio — volver al texto por defecto.
        settings.AccentColor = Normalize(request.AccentColor);
        settings.FontPairing = Normalize(request.FontPairing);
        settings.HeroTitle = Normalize(request.HeroTitle);
        settings.HeroSubtitle = Normalize(request.HeroSubtitle);
        settings.AboutText = Normalize(request.AboutText);
        settings.FooterTagline = Normalize(request.FooterTagline);
        settings.UpdatedAt = DateTimeOffset.UtcNow;

        await _repo.SaveChangesAsync(ct);
        return SiteSettingsDto.From(settings);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
