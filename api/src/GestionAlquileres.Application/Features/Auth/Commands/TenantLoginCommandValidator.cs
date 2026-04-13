using FluentValidation;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class TenantLoginCommandValidator : AbstractValidator<TenantLoginCommand>
{
    public TenantLoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.OrganizationSlug).NotEmpty();
    }
}
