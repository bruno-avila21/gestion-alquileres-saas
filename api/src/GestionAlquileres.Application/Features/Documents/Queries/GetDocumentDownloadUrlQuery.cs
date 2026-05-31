using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Queries;

/// <summary>
/// Requests a short-lived signed download URL for a document.
/// Ownership is enforced in the handler: staff may access any document in their org,
/// a Tenant may only access documents belonging to one of their own contracts.
/// </summary>
public record GetDocumentDownloadUrlQuery(
    Guid DocumentId,
    Guid ContractId,
    Guid RequestingUserId,
    bool IsStaff) : IRequest<DocumentDownloadUrlDto?>;
