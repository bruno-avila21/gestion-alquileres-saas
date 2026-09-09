using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.Public.Commands;
using GestionAlquileres.Application.Features.Public.DTOs;
using GestionAlquileres.Application.Features.Public.Queries;
using GestionAlquileres.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GestionAlquileres.API.Controllers;

/// <summary>
/// Sitio público de cada inmobiliaria. Anónimo a propósito: el tenant sale del slug de la URL
/// (lo resuelve <see cref="Middleware.TenantMiddleware"/>), y las consultas sólo devuelven
/// publicaciones en estado Published.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/public/{slug}")]
public class PublicController : ControllerBase
{
    private readonly ISender _mediator;
    public PublicController(ISender mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PublicOrganizationDto>> GetOrganization(string slug, CancellationToken ct)
    {
        var org = await _mediator.Send(new GetPublicOrganizationQuery(slug), ct);
        return org is null ? NotFound() : Ok(org);
    }

    [HttpGet("listings")]
    public async Task<ActionResult<PublicListingSearchResultDto>> Search(
        string slug,
        [FromQuery] OperationType? operation,
        [FromQuery] PropertyType? type,
        [FromQuery] string? city,
        [FromQuery] string? neighborhood,
        [FromQuery] Currency? currency,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] int? minRooms,
        [FromQuery] int? minBedrooms,
        [FromQuery] decimal? minArea,
        [FromQuery] decimal? maxArea,
        [FromQuery] string[]? features,
        [FromQuery] bool? credit,
        [FromQuery] string? sort,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 24,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new SearchPublicListingsQuery(
            slug, operation, type, city, neighborhood, currency, minPrice, maxPrice,
            minRooms, minBedrooms, minArea, maxArea, features, credit, sort, page, pageSize), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("listings/{id:guid}")]
    public async Task<ActionResult<PublicListingDetailDto>> GetListing(string slug, Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicListingQuery(slug, id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("photos/{photoId:guid}")]
    [ResponseCache(Duration = 86_400, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetPhoto(string slug, Guid photoId, CancellationToken ct)
    {
        var file = await _mediator.Send(new GetPublicPhotoQuery(photoId), ct);
        return file is null ? NotFound() : File(file.Content, file.MimeType);
    }

    /// <summary>
    /// Consulta desde el formulario público (ficha de una publicación o "Contacto" del home).
    /// <paramref name="request"/>.Website es el honeypot: si viene con contenido, se descarta en
    /// silencio (204) sin tocar la base ni revelar que fue detectado como bot.
    /// </summary>
    [HttpPost("leads")]
    [EnableRateLimiting("public-leads")]
    public async Task<IActionResult> CreateLead(string slug, [FromBody] CreatePublicLeadRequest request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.Website))
            return NoContent();

        var id = await _mediator.Send(
            new CreatePublicLeadCommand(slug, request.Name, request.Email, request.Phone, request.Message, request.ListingId), ct);
        return id is null ? NotFound() : StatusCode(StatusCodes.Status201Created, new { id });
    }
}
