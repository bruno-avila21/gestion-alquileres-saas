using GestionAlquileres.Application.Features.Organizations.DTOs;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Organizations.Commands;

public class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, OrganizationDto>
{
    private readonly IOrganizationRepository _repo;
    private readonly ICurrentTenant _currentTenant;

    public UpdateOrganizationCommandHandler(IOrganizationRepository repo, ICurrentTenant currentTenant)
    {
        _repo = repo;
        _currentTenant = currentTenant;
    }

    public async Task<OrganizationDto> Handle(UpdateOrganizationCommand request, CancellationToken ct)
    {
        var org = await _repo.GetByIdAsync(_currentTenant.OrganizationId, ct)
            ?? throw new InvalidOperationException("La organización del token no existe.");

        org.Name = request.Name.Trim();
        org.LegalName = Normalize(request.LegalName);
        org.TaxId = Normalize(request.TaxId);
        org.Address = Normalize(request.Address);
        org.Phone = Normalize(request.Phone);
        org.Email = Normalize(request.Email);
        org.BrandColor = Normalize(request.BrandColor);

        await _repo.SaveChangesAsync(ct);

        return OrganizationDto.From(org);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
