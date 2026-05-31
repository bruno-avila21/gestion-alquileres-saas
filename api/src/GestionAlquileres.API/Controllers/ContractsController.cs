using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.Contracts.Commands;
using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Application.Features.Contracts.Queries;
using GestionAlquileres.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/contracts")]
public class ContractsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ContractDto>>> GetAll(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? propertyId,
        [FromQuery] ContractStatus? status,
        CancellationToken ct) =>
        Ok(await Mediator.Send(new ListContractsQuery(tenantId, propertyId, status), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ContractDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetContractByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ContractDto>> Create(
        [FromBody] CreateContractRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateContractCommand(
            OrganizationId, request.PropertyId, request.AppTenantId,
            request.StartDate, request.EndDate, request.MonthlyRent,
            request.Currency, request.AdjustmentType, request.AdjustmentFrequency,
            request.DayOfMonth, request.DepositAmount, request.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ContractDto>> Update(
        Guid id, [FromBody] UpdateContractRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateContractCommand(
            id, request.PropertyId, request.AppTenantId,
            request.StartDate, request.EndDate, request.MonthlyRent,
            request.Currency, request.AdjustmentType, request.AdjustmentFrequency,
            request.DayOfMonth, request.DepositAmount, request.Notes), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/terminate")]
    public async Task<ActionResult<ContractDto>> Terminate(
        Guid id, [FromBody] TerminateContractRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new TerminateContractCommand(id, request.Notes), ct);
        return result is null ? NotFound() : Ok(result);
    }
}
