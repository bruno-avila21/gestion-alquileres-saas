using System.Text;
using GestionAlquileres.API.Common;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Application.Features.Transactions.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/transactions")]
public class TransactionsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> ListAll(CancellationToken ct) =>
        Ok(await Mediator.Send(new ListAllTransactionsQuery(), ct));

    [HttpGet("export")]
    public async Task<IActionResult> Export(CancellationToken ct)
    {
        var txs = await Mediator.Send(new ListAllTransactionsQuery(), ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,ContractId,Tipo,Importe,Moneda,Periodo,Notas,FechaCreacion");

        foreach (var t in txs)
        {
            sb.AppendLine(
                $"{t.Id},{t.ContractId},{t.Type},{Csv.Number(t.Amount)},{t.Currency}," +
                $"{t.Period:yyyy-MM-dd},{Csv.Field(t.Notes)},{t.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/csv; charset=utf-8", "transacciones.csv");
    }
}
