using GestionAlquileres.Application.Common.Export;
using System.Text;
using GestionAlquileres.API.Common;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Application.Features.Transactions.Queries;
using GestionAlquileres.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/transactions")]
public class TransactionsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TransactionsPageDto>> ListAll(
        CancellationToken ct, int page = 1, int pageSize = 20, TransactionType? type = null, string? search = null) =>
        Ok(await Mediator.Send(new GetTransactionsPageQuery(page, pageSize, type, search), ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var txs = await Mediator.Send(new ListAllTransactionsQuery(), ct);

        // Se piden MaxRows + 1: si vinieron todas, había al menos una más y el archivo va recortado.
        var truncated = txs.Count > ExportLimits.MaxRows;
        var rows = truncated ? txs.Take(ExportLimits.MaxRows) : txs;

        var sb = new StringBuilder();
        sb.AppendLine("Id,ContractId,Tipo,Importe,Moneda,Periodo,Notas,FechaCreacion");

        foreach (var t in rows)
        {
            sb.AppendLine(
                $"{t.Id},{t.ContractId},{t.Type},{Csv.Number(t.Amount)},{t.Currency}," +
                $"{t.Period:yyyy-MM-dd},{Csv.Field(t.Notes)},{t.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}");
        }

        if (truncated)
        {
            sb.AppendLine(ExportLimits.TruncationNotice("transacciones"));
            Response.Headers[ExportLimits.TruncatedHeader] = "true";
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", "transacciones.csv");
    }
}
