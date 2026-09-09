using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Domain.Entities;

/// <summary>
/// Registro de una notificación ya enviada, para que los jobs recurrentes sean idempotentes.
///
/// Sin esto, <c>ContractExpiryNotificationJob</c> corre todos los días sobre una ventana de 30 días
/// y le manda el MISMO aviso al mismo inquilino hasta 30 veces seguidas.
///
/// <see cref="DedupeKey"/> distingue un evento lógico de otro dentro del mismo <see cref="Kind"/>.
/// Para el vencimiento de contrato la clave es la fecha de fin: si el contrato se renueva con una
/// fecha nueva, corresponde volver a avisar; mientras la fecha no cambie, se avisa una sola vez.
/// </summary>
public class SentNotification : ITenantEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OrganizationId { get; set; }
    public Guid ContractId { get; set; }
    public NotificationKind Kind { get; set; }
    public string DedupeKey { get; set; } = "";
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Clave de deduplicación del aviso de vencimiento: la fecha de fin del contrato.</summary>
    public static string ExpiryKey(DateOnly endDate) => endDate.ToString("yyyy-MM-dd");
}
