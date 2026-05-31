using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Queries;

public class ListContractDocumentsQueryHandler : IRequestHandler<ListContractDocumentsQuery, IReadOnlyList<DocumentDto>>
{
    private readonly IDocumentRepository _docs;

    public ListContractDocumentsQueryHandler(IDocumentRepository docs) => _docs = docs;

    public async Task<IReadOnlyList<DocumentDto>> Handle(ListContractDocumentsQuery req, CancellationToken ct)
    {
        var docs = await _docs.GetByContractAsync(req.ContractId, ct);
        return docs.Select(d => new DocumentDto(d.Id, d.ContractId, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt))
                   .ToList();
    }
}
