using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Leads.Commands;
using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Application.Features.Leads.Queries;
using GestionAlquileres.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

/// <summary>CRM de consultas (leads) — bloque A3. Tablero de estados para el seguimiento comercial.</summary>
[Route("api/v1/leads")]
public class LeadsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<LeadDto>>> GetAll(
        [FromQuery] LeadStatus? status, [FromQuery] string? search,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default) =>
        Ok(await Mediator.Send(new GetLeadsPageQuery(status, search, page, pageSize), ct));

    [HttpGet("summary")]
    public async Task<ActionResult<LeadSummaryDto>> GetSummary(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetLeadSummaryQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LeadDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetLeadByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<LeadDto>> Create([FromBody] CreateLeadRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(
            new CreateLeadCommand(request.Name, request.Email, request.Phone, request.Message, request.ListingId), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LeadDto>> Update(Guid id, [FromBody] UpdateLeadRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new UpdateLeadCommand(id, request.Name, request.Email, request.Phone, request.Message), ct));

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<LeadDto>> ChangeStatus(Guid id, [FromBody] ChangeLeadStatusRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new ChangeLeadStatusCommand(id, request.Status, request.LostReason), ct));

    [HttpPost("{id:guid}/notes")]
    public async Task<ActionResult<LeadNoteDto>> AddNote(Guid id, [FromBody] AddLeadNoteRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new AddLeadNoteCommand(id, request.Text, CurrentUserId), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteLeadCommand(id), ct);
        return NoContent();
    }
}
