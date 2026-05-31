namespace GestionAlquileres.Application.Features.Documents;

public record DocumentDto(
    Guid Id,
    Guid ContractId,
    string FileName,
    string MimeType,
    long SizeBytes,
    DateTimeOffset CreatedAt);

public record DocumentDownloadUrlDto(string Token, string Url, DateTimeOffset ExpiresAt);
