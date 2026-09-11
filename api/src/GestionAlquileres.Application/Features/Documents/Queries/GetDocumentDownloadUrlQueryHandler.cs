using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Queries;

public class GetDocumentDownloadUrlQueryHandler
    : IRequestHandler<GetDocumentDownloadUrlQuery, DocumentDownloadUrlDto?>
{
    private readonly IDocumentRepository _docs;
    private readonly IAppTenantRepository _tenants;
    private readonly IContractRepository _contracts;
    private readonly IDocumentTokenService _tokenSvc;

    public GetDocumentDownloadUrlQueryHandler(
        IDocumentRepository docs,
        IAppTenantRepository tenants,
        IContractRepository contracts,
        IDocumentTokenService tokenSvc)
    {
        _docs = docs;
        _tenants = tenants;
        _contracts = contracts;
        _tokenSvc = tokenSvc;
    }

    public async Task<DocumentDownloadUrlDto?> Handle(GetDocumentDownloadUrlQuery req, CancellationToken ct)
    {
        // GetByIdAsync applies the global multi-tenant filter (same OrganizationId only).
        var doc = await _docs.GetByIdAsync(req.DocumentId, ct);
        if (doc is null) return null;

        // The document must belong to the contract addressed in the route — prevents
        // enumerating other contracts' documents through a mismatched route.
        if (doc.ContractId != req.ContractId) return null;

        // Tenants may only download documents from one of their own contracts, and only those an
        // admin has shared with them (audit A2). Staff (Admin/Staff) may download any document
        // within the organization, visible or not.
        if (!req.IsStaff)
        {
            if (!doc.IsVisibleToTenant) return null;

            var appTenant = await _tenants.GetByUserIdAsync(req.RequestingUserId, ct);
            if (appTenant is null) return null;

            var ownContracts = await _contracts.ListAsync(appTenant.Id, null, null, ct);
            if (ownContracts.All(c => c.Id != doc.ContractId)) return null;
        }

        var token = _tokenSvc.Generate(doc.Id, doc.OrganizationId);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_tokenSvc.ExpiryMinutes);
        return new DocumentDownloadUrlDto(token, $"/api/v1/files/download?token={token}", expiresAt);
    }
}
