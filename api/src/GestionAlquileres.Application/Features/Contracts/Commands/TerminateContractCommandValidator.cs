using FluentValidation;

namespace GestionAlquileres.Application.Features.Contracts.Commands;

public class TerminateContractCommandValidator : AbstractValidator<TerminateContractCommand>
{
    public TerminateContractCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
