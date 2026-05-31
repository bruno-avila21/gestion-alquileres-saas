using GestionAlquileres.Application.Features.Documents;
using MediatR;

namespace GestionAlquileres.Application.Features.Me;

public record GetMyDocumentsQuery(Guid UserId) : IRequest<IReadOnlyList<DocumentDto>>;
