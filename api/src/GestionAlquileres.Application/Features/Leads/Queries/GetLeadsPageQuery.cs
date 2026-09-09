using FluentValidation;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Paging;
using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Leads.Queries;

public record GetLeadsPageQuery(LeadStatus? Status, string? Search, int Page, int PageSize)
    : IRequest<PagedResult<LeadDto>>;

public class GetLeadsPageQueryValidator : AbstractValidator<GetLeadsPageQuery>
{
    public GetLeadsPageQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status.HasValue);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}

public class GetLeadsPageQueryHandler : IRequestHandler<GetLeadsPageQuery, PagedResult<LeadDto>>
{
    private readonly ILeadRepository _leads;
    public GetLeadsPageQueryHandler(ILeadRepository leads) => _leads = leads;

    public async Task<PagedResult<LeadDto>> Handle(GetLeadsPageQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);
        var (items, total) = await _leads.GetPagedAsync(request.Status, request.Search, page, pageSize, ct);
        var dtos = items.Select(LeadDto.From).ToList();
        return new PagedResult<LeadDto>(dtos, total, page, pageSize);
    }
}
