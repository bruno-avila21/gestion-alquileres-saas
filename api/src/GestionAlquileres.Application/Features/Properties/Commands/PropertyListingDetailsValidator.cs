using FluentValidation;
using GestionAlquileres.Application.Features.Properties.DTOs;

namespace GestionAlquileres.Application.Features.Properties.Commands;

/// <summary>Reglas de la ficha pública, compartidas por alta y edición.</summary>
public class PropertyListingDetailsValidator : AbstractValidator<PropertyListingDetails>
{
    public const int MaxFeatures = 40;
    public const int MaxFeatureLength = 40;

    public PropertyListingDetailsValidator()
    {
        RuleFor(x => x.Neighborhood).MaximumLength(100);
        RuleFor(x => x.Code).MaximumLength(30);
        RuleFor(x => x.Description).MaximumLength(5000);
        RuleFor(x => x.Rooms).InclusiveBetween(0, 50).When(x => x.Rooms.HasValue);
        RuleFor(x => x.Bedrooms).InclusiveBetween(0, 50).When(x => x.Bedrooms.HasValue);
        RuleFor(x => x.Bathrooms).InclusiveBetween(0, 50).When(x => x.Bathrooms.HasValue);
        RuleFor(x => x.Garages).InclusiveBetween(0, 50).When(x => x.Garages.HasValue);
        RuleFor(x => x.AgeYears).InclusiveBetween(0, 300).When(x => x.AgeYears.HasValue);
        RuleFor(x => x.CoveredAreaM2).GreaterThan(0).When(x => x.CoveredAreaM2.HasValue);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.Features!)
            .Must(f => f.Count <= MaxFeatures).WithMessage($"Máximo {MaxFeatures} características.")
            .Must(f => f.All(s => !string.IsNullOrWhiteSpace(s) && s.Length <= MaxFeatureLength && !s.Contains('|')))
            .WithMessage($"Cada característica tiene hasta {MaxFeatureLength} caracteres y no puede contener '|'.")
            .When(x => x.Features is not null);
    }
}
