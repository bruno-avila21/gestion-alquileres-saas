namespace GestionAlquileres.Domain.Interfaces.Services;

public interface IDocumentTokenService
{
    string Generate(Guid documentId, Guid organizationId);
    (Guid documentId, Guid organizationId)? Validate(string token);
    int ExpiryMinutes { get; }
}
