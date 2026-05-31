using FluentValidation;

namespace GestionAlquileres.Application.Features.Properties.Commands;

public class DeletePropertyCommandValidator : AbstractValidator<DeletePropertyCommand>
{
    public DeletePropertyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
