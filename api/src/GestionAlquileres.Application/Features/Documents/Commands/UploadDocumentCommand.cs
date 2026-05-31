using GestionAlquileres.Application.Features.Documents;
using MediatR;

namespace GestionAlquileres.Application.Features.Documents.Commands;

public record UploadDocumentCommand(
    Guid ContractId,
    string FileName,
    string MimeType,
    long SizeBytes,
    Stream Content,
    Guid UploadedByUserId) : IRequest<DocumentDto>;
