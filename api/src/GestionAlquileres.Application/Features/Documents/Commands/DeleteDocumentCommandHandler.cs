using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Commands;

public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IDocumentRepository _docs;
    private readonly IStorageService _storage;

    public DeleteDocumentCommandHandler(IDocumentRepository docs, IStorageService storage)
    {
        _docs = docs;
        _storage = storage;
    }

    public async Task Handle(DeleteDocumentCommand req, CancellationToken ct)
    {
        var doc = await _docs.GetByIdAsync(req.DocumentId, ct)
            ?? throw new KeyNotFoundException($"Document {req.DocumentId} not found.");

        await _storage.DeleteAsync(doc.StorageKey, ct);
        await _docs.RemoveAsync(doc, ct);
        await _docs.SaveChangesAsync(ct);
    }
}
