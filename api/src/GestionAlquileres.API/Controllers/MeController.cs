using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Application.Features.Documents;
using GestionAlquileres.Application.Features.Me;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/me")]
public class MeController : BaseController
{
    [HttpGet("contract")]
    public async Task<ActionResult<ContractDto?>> GetMyContract(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetMyContractQuery(CurrentUserId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetMyTransactions(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMyTransactionsQuery(CurrentUserId), ct));

    [HttpGet("documents")]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> GetMyDocuments(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMyDocumentsQuery(CurrentUserId), ct));

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<RentHistoryDto>>> GetMyRentHistory(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetMyRentHistoryQuery(CurrentUserId), ct));
}
