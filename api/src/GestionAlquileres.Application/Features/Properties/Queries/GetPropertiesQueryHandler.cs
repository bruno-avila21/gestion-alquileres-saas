using GestionAlquileres.Application.Features.Properties.Commands;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Queries;

public class GetPropertiesQueryHandler : IRequestHandler<GetPropertiesQuery, IReadOnlyList<PropertyDto>>
{
    private readonly IPropertyRepository _repo;

    public GetPropertiesQueryHandler(IPropertyRepository repo) => _repo = repo;

    public async Task<IReadOnlyList<PropertyDto>> Handle(GetPropertiesQuery request, CancellationToken ct)
    {
        var properties = await _repo.GetAllAsync(ct);
        return properties.Select(CreatePropertyCommandHandler.ToDto).ToList();
    }
}
