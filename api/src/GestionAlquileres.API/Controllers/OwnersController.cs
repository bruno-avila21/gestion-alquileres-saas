using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.Owners.Commands;
using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Application.Features.Owners.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/owners")]
public class OwnersController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<OwnerDto>>> GetAll(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetOwnersQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OwnerDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOwnerByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<OwnerDto>> Create([FromBody] CreateOwnerRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateOwnerCommand(
            OrganizationId, request.Name, request.TaxId, request.Email, request.Phone, request.Cbu, request.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<OwnerDto>> Update(Guid id, [FromBody] UpdateOwnerRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateOwnerCommand(
            id, request.Name, request.TaxId, request.Email, request.Phone, request.Cbu, request.Notes, request.IsActive), ct);
        return Ok(result);
    }

    /// <summary>Monthly settlement (rendición) for the owner over the inclusive period range.</summary>
    [HttpGet("{ownerId:guid}/settlement")]
    public async Task<ActionResult<OwnerSettlementDto>> GetSettlement(
        Guid ownerId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetOwnerSettlementQuery(ownerId, from, to), ct));

    /// <summary>La misma liquidación, en PDF. Propietario inexistente -> 404; to &lt; from -> 409.</summary>
    [HttpGet("{ownerId:guid}/settlement/pdf")]
    public async Task<IActionResult> GetSettlementPdf(
        Guid ownerId, [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var pdf = await Mediator.Send(new GetOwnerSettlementPdfQuery(ownerId, from, to), ct);
        return pdf is null ? NotFound() : File(pdf.Content, "application/pdf", pdf.FileName);
    }
}
