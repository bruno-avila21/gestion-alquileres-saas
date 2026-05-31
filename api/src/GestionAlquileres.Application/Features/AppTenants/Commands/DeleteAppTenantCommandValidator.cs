using FluentValidation;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class DeleteAppTenantCommandValidator : AbstractValidator<DeleteAppTenantCommand>
{
    public DeleteAppTenantCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
