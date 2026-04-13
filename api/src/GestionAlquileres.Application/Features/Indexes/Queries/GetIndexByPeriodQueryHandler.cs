using GestionAlquileres.Application.Features.Indexes.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Indexes.Queries;

public class GetIndexByPeriodQueryHandler
    : IRequestHandler<GetIndexByPeriodQuery, IReadOnlyList<IndexValueDto>>
{
    private readonly IIndexRepository _indexRepo;

    public GetIndexByPeriodQueryHandler(IIndexRepository indexRepo) => _indexRepo = indexRepo;

    public async Task<IReadOnlyList<IndexValueDto>> Handle(
        GetIndexByPeriodQuery request, CancellationToken ct)
    {
        var rows = await _indexRepo.GetRangeAsync(request.IndexType, request.From, request.To, ct);
        return rows
            .Select(v => new IndexValueDto(
                v.Id, v.IndexType, v.Period, v.Value, v.VariationPct, v.Source, v.FetchedAt))
            .ToList();
    }
}
