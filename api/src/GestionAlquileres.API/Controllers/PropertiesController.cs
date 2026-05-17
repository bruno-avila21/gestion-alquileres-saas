using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.Properties.Commands;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Application.Features.Properties.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/properties")]
public class PropertiesController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> GetAll(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPropertiesQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetPropertyByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<PropertyDto>> Create(
        [FromBody] CreatePropertyRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreatePropertyCommand(
            OrganizationId, request.Address, request.City, request.Province,
            request.PropertyType, request.AreaM2, request.Notes), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PropertyDto>> Update(
        Guid id, [FromBody] UpdatePropertyRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdatePropertyCommand(
            id, request.Address, request.City, request.Province,
            request.PropertyType, request.AreaM2, request.Notes, request.IsActive), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeletePropertyCommand(id), ct);
        return NoContent();
    }
}
