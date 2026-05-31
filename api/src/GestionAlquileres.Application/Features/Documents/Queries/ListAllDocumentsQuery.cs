using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Queries;

public record ListAllDocumentsQuery : IRequest<IReadOnlyList<DocumentDto>>;
