using GestionAlquileres.Application.Common.Export;
using System.Text;
using GestionAlquileres.API.Common;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Application.Features.RentHistory.Queries;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/rent-adjustments")]
public class RentAdjustmentsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<RentHistoryDto>>> ListAll(
        CancellationToken ct, int page = 1, int pageSize = 20, AdjustmentType? type = null, string? search = null) =>
        Ok(await Mediator.Send(new GetRentHistoryPageQuery(page, pageSize, type, search), ct));

    /// <summary>
    /// Simula un ajuste sobre parámetros sueltos, sin contrato y sin escribir nada.
    /// 204 para tipo Manual, que no tiene fórmula que simular.
    /// </summary>
    [HttpGet("simulate")]
    public async Task<ActionResult<AdjustmentProjection>> Simulate(
        [FromQuery] AdjustmentType type,
        [FromQuery] decimal initialRent,
        [FromQuery] DateOnly startDate,
        [FromQuery] AdjustmentFrequency frequency,
        CancellationToken ct,
        [FromQuery] decimal? percent = null,
        [FromQuery] DateOnly? until = null)
    {
        var result = await Mediator.Send(
            new SimulateAdjustmentQuery(type, initialRent, startDate, frequency, percent, until), ct);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var records = await Mediator.Send(new ListAllRentHistoryQuery(), ct);

        // Se piden MaxRows + 1: si vinieron todas, había al menos una más y el archivo va recortado.
        var truncated = records.Count > ExportLimits.MaxRows;
        var rows = truncated ? records.Take(ExportLimits.MaxRows) : records;

        var sb = new StringBuilder();
        sb.AppendLine("Id,ContractId,TipoAjuste,AlquilerAnterior,NuevoAlquiler,Factor,FechaVigencia,Notas,FechaCreacion");

        foreach (var r in rows)
        {
            sb.AppendLine(
                $"{r.Id},{r.ContractId},{r.AdjustmentType}," +
                $"{Csv.Number(r.PreviousRent)},{Csv.Number(r.NewRent)},{Csv.Number(r.AdjustmentFactor, "F6")}," +
                $"{r.EffectiveDate:yyyy-MM-dd},{Csv.Field(r.Notes)},{r.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}");
        }

        if (truncated)
        {
            sb.AppendLine(ExportLimits.TruncationNotice("actualizaciones"));
            Response.Headers[ExportLimits.TruncatedHeader] = "true";
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", "ajustes.csv");
    }
}
