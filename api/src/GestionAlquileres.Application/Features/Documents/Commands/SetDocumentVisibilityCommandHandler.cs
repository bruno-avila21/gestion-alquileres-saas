using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Commands;

public class SetDocumentVisibilityCommandHandler : IRequestHandler<SetDocumentVisibilityCommand, DocumentDto?>
{
    private readonly IDocumentRepository _docs;

    public SetDocumentVisibilityCommandHandler(IDocumentRepository docs) => _docs = docs;

    public async Task<DocumentDto?> Handle(SetDocumentVisibilityCommand req, CancellationToken ct)
    {
        // GetByIdAsync applies the multi-tenant filter, so a cross-org id resolves to null.
        var doc = await _docs.GetByIdAsync(req.DocumentId, ct);
        if (doc is null) return null;

        // Ownership is decided HERE, before any mutation. The controller used to check this
        // after the command had already saved, so a staff member could flip visibility on a
        // document belonging to another contract and still get a 404 back — while the tenant
        // of that other contract could already download it via /me/documents.
        if (doc.ContractId != req.ContractId) return null;

        doc.IsVisibleToTenant = req.IsVisibleToTenant;
        await _docs.SaveChangesAsync(ct);

        return new DocumentDto(doc.Id, doc.ContractId, doc.FileName, doc.MimeType, doc.SizeBytes, doc.CreatedAt, doc.IsVisibleToTenant);
    }
}
