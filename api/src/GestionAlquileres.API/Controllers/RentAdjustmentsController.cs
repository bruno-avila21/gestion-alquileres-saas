using System.Text;
using GestionAlquileres.API.Common;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Application.Features.RentHistory.Queries;
using GestionAlquileres.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/rent-adjustments")]
public class RentAdjustmentsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<RentHistoryDto>>> ListAll(
        CancellationToken ct, int page = 1, int pageSize = 20, AdjustmentType? type = null, string? search = null) =>
        Ok(await Mediator.Send(new GetRentHistoryPageQuery(page, pageSize, type, search), ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var records = await Mediator.Send(new ListAllRentHistoryQuery(), ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,ContractId,TipoAjuste,AlquilerAnterior,NuevoAlquiler,Factor,FechaVigencia,Notas,FechaCreacion");

        foreach (var r in records)
        {
            sb.AppendLine(
                $"{r.Id},{r.ContractId},{r.AdjustmentType}," +
                $"{Csv.Number(r.PreviousRent)},{Csv.Number(r.NewRent)},{Csv.Number(r.AdjustmentFactor, "F6")}," +
                $"{r.EffectiveDate:yyyy-MM-dd},{Csv.Field(r.Notes)},{r.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", "ajustes.csv");
    }
}
