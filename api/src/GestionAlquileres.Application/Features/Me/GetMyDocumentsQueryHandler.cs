using GestionAlquileres.Application.Features.Documents;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Me;

public class GetMyDocumentsQueryHandler : IRequestHandler<GetMyDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    private readonly IAppTenantRepository _tenantRepo;
    private readonly IContractRepository _contractRepo;
    private readonly IDocumentRepository _docRepo;

    public GetMyDocumentsQueryHandler(
        IAppTenantRepository tenantRepo,
        IContractRepository contractRepo,
        IDocumentRepository docRepo)
    {
        _tenantRepo = tenantRepo;
        _contractRepo = contractRepo;
        _docRepo = docRepo;
    }

    public async Task<IReadOnlyList<DocumentDto>> Handle(GetMyDocumentsQuery request, CancellationToken ct)
    {
        var appTenant = await _tenantRepo.GetByUserIdAsync(request.UserId, ct);
        if (appTenant is null) return [];

        var contracts = await _contractRepo.ListAsync(appTenant.Id, null, ContractStatus.Active, ct);
        var contract = contracts.FirstOrDefault();
        if (contract is null) return [];

        // Tenants only ever see documents an admin has explicitly shared (audit A2).
        var docs = await _docRepo.GetByContractAsync(contract.Id, ct);
        return docs.Where(d => d.IsVisibleToTenant)
                   .Select(d => new DocumentDto(d.Id, d.ContractId, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt, d.IsVisibleToTenant))
                   .ToList();
    }
}
