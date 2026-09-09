using FluentValidation;

namespace GestionAlquileres.Application.Features.Listings.Commands;

public class CreateListingCommandValidator : AbstractValidator<CreateListingCommand>
{
    public CreateListingCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.OperationType).IsInEnum();
        RuleFor(x => x.Currency).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Expenses).GreaterThanOrEqualTo(0).When(x => x.Expenses.HasValue);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public class UpdateListingCommandValidator : AbstractValidator<UpdateListingCommand>
{
    public UpdateListingCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.OperationType).IsInEnum();
        RuleFor(x => x.Currency).IsInEnum();
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Expenses).GreaterThanOrEqualTo(0).When(x => x.Expenses.HasValue);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
    }
}

public class DeleteListingCommandValidator : AbstractValidator<DeleteListingCommand>
{
    public DeleteListingCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
