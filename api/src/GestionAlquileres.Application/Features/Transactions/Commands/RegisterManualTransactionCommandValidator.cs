using FluentValidation;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Application.Features.Transactions.Commands;

public class RegisterManualTransactionCommandValidator : AbstractValidator<RegisterManualTransactionCommand>
{
    public RegisterManualTransactionCommandValidator()
    {
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("El importe debe ser mayor a 0.");
        RuleFor(x => x.Period).NotEmpty();
        RuleFor(x => x.Notes).NotEmpty().WithMessage("Las transacciones manuales requieren una nota explicativa.");
        RuleFor(x => x.Type)
            .Must(t => t == TransactionType.ManualDebit || t == TransactionType.ManualCredit)
            .WithMessage("El tipo debe ser ManualDebit o ManualCredit.");
    }
}
