using GestionAlquileres.Domain.Reports;

namespace GestionAlquileres.Domain.Interfaces.Services;

/// <summary>
/// Genera los PDF del negocio (recibo de pago, liquidación al propietario) a partir de modelos de
/// dominio ya resueltos. La implementación concreta vive en Infrastructure (QuestPDF): Application
/// no puede referenciar librerías de terceros, así que sólo conoce esta interfaz.
/// </summary>
public interface IPdfReportGenerator
{
    byte[] RenderReceipt(ReceiptReport report);
    byte[] RenderOwnerSettlement(OwnerSettlementReport report);
}
