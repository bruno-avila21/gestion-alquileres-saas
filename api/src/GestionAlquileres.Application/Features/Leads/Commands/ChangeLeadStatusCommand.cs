using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Leads.Commands;

public record ChangeLeadStatusCommand(Guid Id, LeadStatus Status, string? LostReason) : IRequest<LeadDto>;

public class ChangeLeadStatusCommandHandler : IRequestHandler<ChangeLeadStatusCommand, LeadDto>
{
    private readonly ILeadRepository _leads;
    public ChangeLeadStatusCommandHandler(ILeadRepository leads) => _leads = leads;

    public async Task<LeadDto> Handle(ChangeLeadStatusCommand request, CancellationToken ct)
    {
        var lead = await _leads.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Lead {request.Id} not found.");

        // LostReason-required-when-Lost is enforced by ChangeLeadStatusCommandValidator (400) before
        // this handler ever runs — the ValidationBehavior pipeline validates ahead of every handler.

        var now = DateTimeOffset.UtcNow;
        lead.Status = request.Status;
        // Only Lost carries a reason — moving off Lost into any other column clears the stale one.
        lead.LostReason = request.Status == LeadStatus.Lost ? request.LostReason?.Trim() : null;
        lead.LastContactAt = now;
        lead.UpdatedAt = now;

        await _leads.SaveChangesAsync(ct);
        return LeadDto.From(lead);
    }
}
