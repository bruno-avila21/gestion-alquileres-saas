using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Interfaces.Repositories;

public interface ISentNotificationRepository
{
    /// <summary>
    /// ¿Ya se envió esta notificación? Pensado para jobs de background, que corren sin tenant en
    /// contexto: por eso ignora el filtro global y recibe el <paramref name="organizationId"/>
    /// explícito, igual que el resto de los métodos <c>*RawAsync</c> del proyecto.
    /// </summary>
    Task<bool> ExistsRawAsync(
        Guid contractId, Guid organizationId, NotificationKind kind, string dedupeKey, CancellationToken ct);

    Task AddAsync(SentNotification notification, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken ct);
}
