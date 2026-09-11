using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Leads.Queries;

public record GetLeadSummaryQuery : IRequest<LeadSummaryDto>;

public class GetLeadSummaryQueryHandler : IRequestHandler<GetLeadSummaryQuery, LeadSummaryDto>
{
    private readonly ILeadRepository _leads;
    public GetLeadSummaryQueryHandler(ILeadRepository leads) => _leads = leads;

    public async Task<LeadSummaryDto> Handle(GetLeadSummaryQuery request, CancellationToken ct)
    {
        var (total, byStatus) = await _leads.GetSummaryAsync(ct);
        return LeadSummaryDto.From(total, byStatus);
    }
}
