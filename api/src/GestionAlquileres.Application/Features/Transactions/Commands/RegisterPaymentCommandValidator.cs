using FluentValidation;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

public class RegisterPaymentCommandValidator : AbstractValidator<RegisterPaymentCommand>
{
    public RegisterPaymentCommandValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("El importe debe ser mayor a 0.");
        RuleFor(x => x.Period).NotEmpty();
    }
}
