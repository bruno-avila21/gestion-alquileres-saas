using FluentValidation;

namespace GestionAlquileres.Application.Features.Indexes.Queries;

public class GetIndexByPeriodQueryValidator : AbstractValidator<GetIndexByPeriodQuery>
{
    public GetIndexByPeriodQueryValidator()
    {
        RuleFor(x => x.IndexType)
            .IsInEnum()
            .Must(t => (int)t != 0)
            .WithMessage("IndexType must be a valid non-default value.");

        RuleFor(x => x.From)
            .LessThanOrEqualTo(x => x.To)
            .WithMessage("From must be on or before To.");

        RuleFor(x => x.To)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)));
    }
}
