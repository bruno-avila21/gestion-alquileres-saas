using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Queries;

public record ListContractDocumentsQuery(Guid ContractId) : IRequest<IReadOnlyList<DocumentDto>>;
