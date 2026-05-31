using GestionAlquileres.Application.Features.RentHistory.Commands;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.RentHistory.Queries;

public class ListRentHistoryQueryHandler : IRequestHandler<ListRentHistoryQuery, IReadOnlyList<RentHistoryDto>>
{
    private readonly IRentHistoryRepository _repo;
    public ListRentHistoryQueryHandler(IRentHistoryRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<RentHistoryDto>> Handle(ListRentHistoryQuery request, CancellationToken ct)
    {
        var records = await _repo.GetByContractAsync(request.ContractId, ct);
        return records.Select(ApplyRentAdjustmentCommandHandler.ToDto).ToList();
    }
}
