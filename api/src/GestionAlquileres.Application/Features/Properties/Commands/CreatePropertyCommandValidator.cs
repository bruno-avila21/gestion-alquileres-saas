using FluentValidation;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.OrganizationId).NotEmpty();
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.City).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Province).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PropertyType).IsInEnum();
        RuleFor(x => x.AreaM2).GreaterThan(0).When(x => x.AreaM2.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
        RuleFor(x => x.CommissionPct).InclusiveBetween(0, 100).When(x => x.CommissionPct.HasValue);
        RuleFor(x => x.Details!).SetValidator(new PropertyListingDetailsValidator()).When(x => x.Details is not null);
    }
}
