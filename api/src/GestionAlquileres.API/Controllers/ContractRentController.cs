using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.RentHistory.Commands;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Application.Features.RentHistory.Queries;
using GestionAlquileres.Application.Features.Transactions.Commands;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Application.Features.Transactions.Queries;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/contracts/{contractId:guid}")]
public class ContractRentController : AdminControllerBase
{
    [HttpPost("adjust")]
    public async Task<ActionResult<RentHistoryDto>> ApplyAdjustment(
        Guid contractId, [FromBody] ApplyAdjustmentRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new ApplyRentAdjustmentCommand(contractId, request.EffectiveDate, request.ManualNewRent, request.Notes), ct);
        return Ok(result);
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<RentHistoryDto>>> GetHistory(
        Guid contractId, CancellationToken ct) =>
        Ok(await Mediator.Send(new ListRentHistoryQuery(contractId), ct));

    /// <summary>Projected rent schedule under the contract's index (via indices-api). 204 for Manual contracts.</summary>
    [HttpGet("adjustment-projection")]
    public async Task<ActionResult<AdjustmentProjection>> GetAdjustmentProjection(
        Guid contractId, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAdjustmentProjectionQuery(contractId), ct);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IReadOnlyList<TransactionDto>>> GetTransactions(
        Guid contractId, CancellationToken ct) =>
        Ok(await Mediator.Send(new ListTransactionsQuery(contractId), ct));

    [HttpPost("payments")]
    public async Task<ActionResult<TransactionDto>> RegisterPayment(
        Guid contractId, [FromBody] RegisterPaymentRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new RegisterPaymentCommand(contractId, request.Amount, request.Period, request.Notes), ct);
        return Ok(result);
    }

    [HttpPost("charges")]
    public async Task<ActionResult<TransactionDto>> RegisterManualCharge(
        Guid contractId, [FromBody] RegisterManualTransactionRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new RegisterManualTransactionCommand(contractId, request.Type, request.Amount, request.Period, request.Notes), ct);
        return Ok(result);
    }
}
