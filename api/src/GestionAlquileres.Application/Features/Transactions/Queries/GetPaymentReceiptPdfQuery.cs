using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Transactions.Queries;

/// <summary>
/// Recibo de pago en PDF. Null cuando la transacción no existe (o no es de esta organización —
/// el filtro global de tenant la esconde igual): el controller devuelve 404 en ese caso.
/// Si la transacción existe pero no es de tipo Payment, el handler tira BusinessException (409).
/// </summary>
public record GetPaymentReceiptPdfQuery(Guid TransactionId) : IRequest<PdfFileDto?>;
