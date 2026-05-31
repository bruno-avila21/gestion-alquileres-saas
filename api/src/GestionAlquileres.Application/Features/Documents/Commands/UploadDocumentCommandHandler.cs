using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Commands;

public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, DocumentDto>
{
    private readonly IDocumentRepository _docs;
    private readonly IContractRepository _contracts;
    private readonly IStorageService _storage;
    private readonly ICurrentTenant _currentTenant;

    public UploadDocumentCommandHandler(
        IDocumentRepository docs,
        IContractRepository contracts,
        IStorageService storage,
        ICurrentTenant currentTenant)
    {
        _docs = docs;
        _contracts = contracts;
        _storage = storage;
        _currentTenant = currentTenant;
    }

    public async Task<DocumentDto> Handle(UploadDocumentCommand req, CancellationToken ct)
    {
        var contract = await _contracts.GetByIdAsync(req.ContractId, ct)
            ?? throw new KeyNotFoundException($"Contract {req.ContractId} not found.");

        var storageKey = await _storage.UploadAsync(req.Content, req.FileName, req.MimeType, ct);

        var doc = new Document
        {
            ContractId   = contract.Id,
            OrganizationId = _currentTenant.OrganizationId,
            FileName     = req.FileName,
            StorageKey   = storageKey,
            MimeType     = req.MimeType,
            SizeBytes    = req.SizeBytes,
            UploadedByUserId = req.UploadedByUserId,
        };

        await _docs.AddAsync(doc, ct);
        await _docs.SaveChangesAsync(ct);

        return new DocumentDto(doc.Id, doc.ContractId, doc.FileName, doc.MimeType, doc.SizeBytes, doc.CreatedAt);
    }
}
