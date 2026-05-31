using FluentValidation;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.RentHistory.Commands;

public class ApplyRentAdjustmentCommandValidator : AbstractValidator<ApplyRentAdjustmentCommand>
{
    public ApplyRentAdjustmentCommandValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.ManualNewRent)
            .GreaterThan(0).WithMessage("El nuevo alquiler manual debe ser mayor a 0.")
            .When(x => x.ManualNewRent.HasValue);
    }
}
