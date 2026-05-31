using GestionAlquileres.Application.Features.RentHistory.Commands;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Queries;

public class ListAllRentHistoryQueryHandler : IRequestHandler<ListAllRentHistoryQuery, IReadOnlyList<RentHistoryDto>>
{
    private readonly IRentHistoryRepository _repo;
    public ListAllRentHistoryQueryHandler(IRentHistoryRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<RentHistoryDto>> Handle(ListAllRentHistoryQuery request, CancellationToken ct)
    {
        var records = await _repo.GetAllAsync(ct);
        return records.Select(ApplyRentAdjustmentCommandHandler.ToDto).ToList();
    }
}
