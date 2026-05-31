using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Commands;

public record DeleteDocumentCommand(Guid DocumentId) : IRequest;
