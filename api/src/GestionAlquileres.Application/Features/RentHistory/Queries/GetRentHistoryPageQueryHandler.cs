using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Paging;
using GestionAlquileres.Application.Features.RentHistory.Commands;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Queries;

public class GetRentHistoryPageQueryHandler
    : IRequestHandler<GetRentHistoryPageQuery, PagedResult<RentHistoryDto>>
{
    private readonly IRentHistoryRepository _repo;
    public GetRentHistoryPageQueryHandler(IRentHistoryRepository repo) => _repo = repo;

    public async Task<PagedResult<RentHistoryDto>> Handle(GetRentHistoryPageQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);
        var (items, total) = await _repo.GetPagedAsync(request.Type, request.Search, page, pageSize, ct);
        var dtos = items.Select(ApplyRentAdjustmentCommandHandler.ToDto).ToList();
        return new PagedResult<RentHistoryDto>(dtos, total, page, pageSize);
    }
}
