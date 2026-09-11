using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Leads.Commands;

public record DeleteLeadCommand(Guid Id) : IRequest;

public class DeleteLeadCommandHandler : IRequestHandler<DeleteLeadCommand>
{
    private readonly ILeadRepository _leads;
    public DeleteLeadCommandHandler(ILeadRepository leads) => _leads = leads;

    public async Task Handle(DeleteLeadCommand request, CancellationToken ct)
    {
        var lead = await _leads.GetForEditAsync(request.Id, ct)
            ?? throw new BusinessException($"Lead {request.Id} not found.");

        _leads.Remove(lead);
        await _leads.SaveChangesAsync(ct);
    }
}
