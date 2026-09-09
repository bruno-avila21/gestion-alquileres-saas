using FluentValidation;

namespace GestionAlquileres.Application.Features.Organizations.Commands;

public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    // #RRGGBB, exactamente lo que pide el contrato.
    private const string HexColorPattern = "^#[0-9A-Fa-f]{6}$";

    public UpdateOrganizationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(x => x.LegalName).MaximumLength(200).When(x => x.LegalName is not null);
        RuleFor(x => x.TaxId).MaximumLength(20).When(x => x.TaxId is not null);
        RuleFor(x => x.Address).MaximumLength(300).When(x => x.Address is not null);
        RuleFor(x => x.Phone).MaximumLength(50).When(x => x.Phone is not null);
        RuleFor(x => x.Email).MaximumLength(200).EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.BrandColor).Matches(HexColorPattern)
            .WithMessage("El color de marca debe tener el formato #RRGGBB.")
            .When(x => !string.IsNullOrWhiteSpace(x.BrandColor));
    }
}
