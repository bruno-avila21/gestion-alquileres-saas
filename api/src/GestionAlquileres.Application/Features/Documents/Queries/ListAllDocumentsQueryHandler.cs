using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Queries;

public class ListAllDocumentsQueryHandler : IRequestHandler<ListAllDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    private readonly IDocumentRepository _docs;
    public ListAllDocumentsQueryHandler(IDocumentRepository docs) => _docs = docs;

    public async Task<IReadOnlyList<DocumentDto>> Handle(ListAllDocumentsQuery request, CancellationToken ct)
    {
        var docs = await _docs.GetAllAsync(ct);
        return docs.Select(d => new DocumentDto(d.Id, d.ContractId, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt))
                   .ToList();
    }
}
