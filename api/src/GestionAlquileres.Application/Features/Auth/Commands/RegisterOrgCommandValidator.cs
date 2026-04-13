using FluentValidation;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class RegisterOrgCommandValidator : AbstractValidator<RegisterOrgCommand>
{
    public RegisterOrgCommandValidator()
    {
        RuleFor(x => x.OrganizationName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lowercase alphanumeric with hyphens.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.AdminPassword).NotEmpty().MinimumLength(8).MaximumLength(100);
        RuleFor(x => x.AdminFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.AdminLastName).NotEmpty().MaximumLength(100);
    }
}
