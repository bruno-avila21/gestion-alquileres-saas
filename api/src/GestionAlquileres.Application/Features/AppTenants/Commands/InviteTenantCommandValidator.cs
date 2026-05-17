using FluentValidation;

namespace GestionAlquileres.Application.Features.AppTenants.Commands;

public class InviteTenantCommandValidator : AbstractValidator<InviteTenantCommand>
{
    public InviteTenantCommandValidator()
    {
        RuleFor(x => x.AppTenantId).NotEmpty();
    }
}
