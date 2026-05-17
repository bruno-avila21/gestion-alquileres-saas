using GestionAlquileres.Application.Features.Properties.Commands;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Properties.Queries;

public class GetPropertyByIdQueryHandler : IRequestHandler<GetPropertyByIdQuery, PropertyDto?>
{
    private readonly IPropertyRepository _repo;

    public GetPropertyByIdQueryHandler(IPropertyRepository repo) => _repo = repo;

    public async Task<PropertyDto?> Handle(GetPropertyByIdQuery request, CancellationToken ct)
    {
        var property = await _repo.GetByIdAsync(request.Id, ct);
        return property is null ? null : CreatePropertyCommandHandler.ToDto(property);
    }
}
