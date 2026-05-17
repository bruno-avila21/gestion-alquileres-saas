using FluentValidation;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class UpdateAppTenantCommandValidator : AbstractValidator<UpdateAppTenantCommand>
{
    public UpdateAppTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Dni).NotEmpty().MaximumLength(15).Matches(@"^\d{7,8}$")
            .WithMessage("El DNI debe contener entre 7 y 8 dígitos numéricos.");
        RuleFor(x => x.Email).EmailAddress().MaximumLength(320).When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone is not null);
    }
}
