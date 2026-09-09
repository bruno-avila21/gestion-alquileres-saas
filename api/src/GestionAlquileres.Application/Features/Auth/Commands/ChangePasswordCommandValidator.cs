using FluentValidation;

namespace GestionAlquileres.Application.Features.Auth.Commands;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    /// <summary>Mismo piso que el alta de organización: 12 caracteres.</summary>
    private const int MinLength = 12;

    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty()
            .WithMessage("Ingresá tu contraseña actual.");

        RuleFor(x => x.NewPassword).NotEmpty()
            .MinimumLength(MinLength)
            .WithMessage($"La contraseña nueva debe tener al menos {MinLength} caracteres.")
            .MaximumLength(100);

        RuleFor(x => x.NewPassword)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("La contraseña nueva tiene que ser distinta de la actual.");
    }
}
