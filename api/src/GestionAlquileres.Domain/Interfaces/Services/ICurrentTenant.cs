namespace GestionAlquileres.Domain.Interfaces.Services;

/// <summary>
/// Exposes the current request's OrganizationId.
/// Returns Guid.Empty for unauthenticated/background requests —
/// callers that require a tenant must validate this explicitly.
/// </summary>
public interface ICurrentTenant
{
    Guid OrganizationId { get; }
}
