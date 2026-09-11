using GestionAlquileres.Application.Common.DTOs;
using MediatR;

namespace GestionAlquileres.Application.Features.Owners.Queries;

/// <summary>
/// Liquidación al propietario en PDF. Null cuando el propietario no existe — el controller
/// devuelve 404 (a diferencia de GetOwnerSettlementQuery, que en JSON responde 409 con
/// "Propietario no encontrado."; esta variante PDF sigue la convención del repo: sólo el GET
/// resuelve con 404). <c>to &lt; from</c> sigue siendo 409 vía BusinessException.
/// </summary>
public record GetOwnerSettlementPdfQuery(Guid OwnerId, DateOnly From, DateOnly To) : IRequest<PdfFileDto?>;
