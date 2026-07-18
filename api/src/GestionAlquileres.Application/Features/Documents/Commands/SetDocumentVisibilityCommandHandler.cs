using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Commands;

public class SetDocumentVisibilityCommandHandler : IRequestHandler<SetDocumentVisibilityCommand, DocumentDto>
{
    private readonly IDocumentRepository _docs;

    public SetDocumentVisibilityCommandHandler(IDocumentRepository docs) => _docs = docs;

    public async Task<DocumentDto> Handle(SetDocumentVisibilityCommand req, CancellationToken ct)
    {
        // GetByIdAsync applies the multi-tenant filter, so a cross-org id resolves to null.
        var doc = await _docs.GetByIdAsync(req.DocumentId, ct)
            ?? throw new KeyNotFoundException($"Document {req.DocumentId} not found.");

        doc.IsVisibleToTenant = req.IsVisibleToTenant;
        await _docs.SaveChangesAsync(ct);

        return new DocumentDto(doc.Id, doc.ContractId, doc.FileName, doc.MimeType, doc.SizeBytes, doc.CreatedAt, doc.IsVisibleToTenant);
    }
}
