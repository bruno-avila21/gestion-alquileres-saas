using FluentValidation;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

public class UpdateContractCommandValidator : AbstractValidator<UpdateContractCommand>
{
    public UpdateContractCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.AppTenantId).NotEmpty();
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty()
            .GreaterThan(x => x.StartDate).WithMessage("La fecha de fin debe ser posterior a la de inicio.");
        RuleFor(x => x.MonthlyRent).GreaterThan(0).WithMessage("El alquiler mensual debe ser mayor a 0.");
        RuleFor(x => x.DayOfMonth).InclusiveBetween(1, 28)
            .WithMessage("El día de cobro debe estar entre 1 y 28.");

        RuleFor(x => x.AdjustmentFrequency).IsInEnum().WithMessage("Frecuencia de ajuste no soportada.");
        RuleFor(x => x.Currency).IsInEnum().WithMessage("Moneda no soportada.");
        RuleFor(x => x.Notes).MaximumLength(2000);

        ContractRules.Apply(this, x => x.AdjustmentType, x => x.AdjustmentPercent);
    }
}
