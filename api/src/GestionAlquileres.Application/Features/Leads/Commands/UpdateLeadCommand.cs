using GestionAlquileres.Application.Common.Exceptions;
using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Leads.Commands;

public record UpdateLeadCommand(Guid Id, string Name, string? Email, string? Phone, string Message)
    : IRequest<LeadDto>;

public class UpdateLeadCommandHandler : IRequestHandler<UpdateLeadCommand, LeadDto>
{
    private readonly ILeadRepository _leads;
    public UpdateLeadCommandHandler(ILeadRepository leads) => _leads = leads;

    public async Task<LeadDto> Handle(UpdateLeadCommand request, CancellationToken ct)
    {
        var lead = await _leads.GetByIdAsync(request.Id, ct)
            ?? throw new BusinessException($"Lead {request.Id} not found.");

        lead.Name = request.Name.Trim();
        lead.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        lead.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        lead.Message = request.Message.Trim();
        lead.UpdatedAt = DateTimeOffset.UtcNow;

        await _leads.SaveChangesAsync(ct);
        return LeadDto.From(lead);
    }
}
