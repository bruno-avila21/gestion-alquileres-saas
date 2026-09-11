using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.Listings.Commands;
using GestionAlquileres.Application.Features.Listings.DTOs;
using GestionAlquileres.Application.Features.Listings.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

/// <summary>Publicaciones (oferta comercial de una propiedad) que administra la inmobiliaria.</summary>
[Route("api/v1/listings")]
public class ListingsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ListingDto>>> GetAll([FromQuery] Guid? propertyId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetListingsQuery(propertyId), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ListingDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetListingByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ListingDto>> Create([FromBody] CreateListingRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateListingCommand(
            request.PropertyId, request.OperationType, request.Price, request.Currency, request.Expenses,
            request.Title, request.IsFeatured, request.Status), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ListingDto>> Update(Guid id, [FromBody] UpdateListingRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new UpdateListingCommand(
            id, request.OperationType, request.Price, request.Currency, request.Expenses,
            request.Title, request.IsFeatured, request.Status), ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteListingCommand(id), ct);
        return NoContent();
    }
}
