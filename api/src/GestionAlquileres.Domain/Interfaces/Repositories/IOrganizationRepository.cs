using GestionAlquileres.Domain.Entities;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id, CancellationToken ct);
    Task<Organization?> GetBySlugAsync(string slug, CancellationToken ct);
    Task<bool> SlugExistsAsync(string slug, CancellationToken ct);
    Task AddAsync(Organization org, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);

    /// <summary>
    /// Incrementa el contador de recibos de la organización de forma atómica y devuelve el nuevo
    /// valor. Dos pedidos concurrentes nunca ven el mismo número ni se saltean uno: el bloqueo de
    /// fila de Postgres serializa el segundo pedido hasta que el primero confirma.
    /// </summary>
    Task<long> IncrementReceiptSequenceAsync(Guid organizationId, CancellationToken ct);
}
