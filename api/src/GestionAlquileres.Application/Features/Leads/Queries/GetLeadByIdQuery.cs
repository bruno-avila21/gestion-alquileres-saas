using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Leads.Queries;

public record GetLeadByIdQuery(Guid Id) : IRequest<LeadDetailDto?>;

public class GetLeadByIdQueryHandler : IRequestHandler<GetLeadByIdQuery, LeadDetailDto?>
{
    private readonly ILeadRepository _leads;
    public GetLeadByIdQueryHandler(ILeadRepository leads) => _leads = leads;

    public async Task<LeadDetailDto?> Handle(GetLeadByIdQuery request, CancellationToken ct)
    {
        // Tenant-filtered: a lead of another organization comes back null, same as not existing.
        var lead = await _leads.GetByIdAsync(request.Id, ct);
        return lead is null ? null : LeadDetailDto.From(lead);
    }
}
