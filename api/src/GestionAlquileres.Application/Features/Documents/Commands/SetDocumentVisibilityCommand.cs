using GestionAlquileres.Application.Features.Documents;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Commands;

/// <summary>Staff-only: share a document with the tenant or make it private again (audit A2).</summary>
public record SetDocumentVisibilityCommand(Guid DocumentId, bool IsVisibleToTenant) : IRequest<DocumentDto>;
