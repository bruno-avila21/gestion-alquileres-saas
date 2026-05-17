using FluentValidation;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.AppTenantId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty()
            .GreaterThan(x => x.StartDate).WithMessage("La fecha de fin debe ser posterior a la de inicio.");
        RuleFor(x => x.MonthlyRent).GreaterThan(0).WithMessage("El alquiler mensual debe ser mayor a 0.");
        RuleFor(x => x.DayOfMonth).InclusiveBetween(1, 28)
            .WithMessage("El día de cobro debe estar entre 1 y 28.");
    }
}
