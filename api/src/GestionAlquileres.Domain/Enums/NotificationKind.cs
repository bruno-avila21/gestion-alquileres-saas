namespace GestionAlquileres.Domain.Enums;

/// <summary>
/// Tipo de notificación saliente. Se persiste como string para que reordenar el enum no
/// reinterprete filas existentes.
/// </summary>
public enum NotificationKind
{
    /// <summary>Aviso al inquilino de que su contrato está por vencer.</summary>
    ContractExpiry = 1,
}
