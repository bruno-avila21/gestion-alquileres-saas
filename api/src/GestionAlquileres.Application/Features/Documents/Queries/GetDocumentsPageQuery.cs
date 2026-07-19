using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Queries;

public record GetDocumentsPageQuery(int Page, int PageSize, string? Search)
    : IRequest<PagedResult<DocumentDto>>;
