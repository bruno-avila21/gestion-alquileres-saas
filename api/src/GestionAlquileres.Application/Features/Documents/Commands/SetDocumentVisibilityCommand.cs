using GestionAlquileres.Application.Features.Documents;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Commands;

/// <summary>
/// Staff-only: share a document with the tenant or make it private again (audit A2).
/// ContractId scopes the document to the route it was requested under: the handler verifies
/// the match BEFORE writing, so a mismatched pair can never flip visibility on another
/// contract's document. Returns null when the document does not exist or does not belong
/// to that contract — the controller maps that to 404.
/// </summary>
public record SetDocumentVisibilityCommand(Guid DocumentId, Guid ContractId, bool IsVisibleToTenant)
    : IRequest<DocumentDto?>;
