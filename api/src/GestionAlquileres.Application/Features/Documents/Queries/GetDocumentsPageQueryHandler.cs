using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Paging;
using GestionAlquileres.Domain.Interfaces.Repositories;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Queries;

public class GetDocumentsPageQueryHandler
    : IRequestHandler<GetDocumentsPageQuery, PagedResult<DocumentDto>>
{
    private readonly IDocumentRepository _docs;
    public GetDocumentsPageQueryHandler(IDocumentRepository docs) => _docs = docs;

    public async Task<PagedResult<DocumentDto>> Handle(GetDocumentsPageQuery request, CancellationToken ct)
    {
        var (page, pageSize) = Paging.Normalize(request.Page, request.PageSize);
        var (items, total) = await _docs.GetPagedAsync(request.Search, page, pageSize, ct);
        var dtos = items
            .Select(d => new DocumentDto(d.Id, d.ContractId, d.FileName, d.MimeType, d.SizeBytes, d.CreatedAt, d.IsVisibleToTenant))
            .ToList();
        return new PagedResult<DocumentDto>(dtos, total, page, pageSize);
    }
}
