using FluentValidation;

namespace GestionAlquileres.Application.Features.Indexes.Commands;

public class SyncIndexCommandValidator : AbstractValidator<SyncIndexCommand>
{
    public SyncIndexCommandValidator()
    {
        RuleFor(x => x.IndexType)
            .IsInEnum()
            .Must(t => (int)t != 0)
            .WithMessage("IndexType must be a valid non-default value.");

        // Period cannot be in the future. Allow "this month" since indices are
        // published mid-month; the sync may be legitimately for today.
        RuleFor(x => x.Period)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Period cannot be in the future.");

        RuleFor(x => x.Period)
            .GreaterThanOrEqualTo(new DateOnly(2000, 1, 1))
            .WithMessage("Period too far in the past.");
    }
}
