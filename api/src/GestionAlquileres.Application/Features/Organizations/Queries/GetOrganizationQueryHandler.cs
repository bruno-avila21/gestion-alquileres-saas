using GestionAlquileres.Application.Features.Organizations.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Queries;

public class GetOrganizationQueryHandler : IRequestHandler<GetOrganizationQuery, OrganizationDto>
{
    private readonly IOrganizationRepository _repo;
    private readonly ICurrentTenant _currentTenant;

    public GetOrganizationQueryHandler(IOrganizationRepository repo, ICurrentTenant currentTenant)
    {
        _repo = repo;
        _currentTenant = currentTenant;
    }

    public async Task<OrganizationDto> Handle(GetOrganizationQuery request, CancellationToken ct)
    {
        var org = await _repo.GetByIdAsync(_currentTenant.OrganizationId, ct)
            ?? throw new InvalidOperationException("La organización del token no existe.");
        return OrganizationDto.From(org);
    }
}
