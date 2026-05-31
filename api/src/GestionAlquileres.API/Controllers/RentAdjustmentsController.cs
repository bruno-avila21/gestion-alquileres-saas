using System.Text;
using GestionAlquileres.API.Common;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Application.Features.RentHistory.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/rent-adjustments")]
public class RentAdjustmentsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RentHistoryDto>>> ListAll(CancellationToken ct) =>
        Ok(await Mediator.Send(new ListAllRentHistoryQuery(), ct));

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
