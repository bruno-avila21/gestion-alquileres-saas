using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.Organizations.Commands;
using GestionAlquileres.Application.Features.Organizations.DTOs;
using GestionAlquileres.Application.Features.Organizations.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

/// <summary>
/// Marca de la inmobiliaria (bloque PDF recibos/liquidaciones, parte A). OrganizationId sale
/// siempre del JWT (BaseController.OrganizationId / ICurrentTenant) — nunca del body ni de la ruta.
/// </summary>
[Route("api/v1/organization")]
public class OrganizationController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<OrganizationDto>> Get(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetOrganizationQuery(), ct));

    [HttpPut]
    public async Task<ActionResult<OrganizationDto>> Update(
        [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateOrganizationCommand(
            request.Name, request.LegalName, request.TaxId, request.Address,
            request.Phone, request.Email, request.BrandColor), ct);
        return Ok(result);
    }

    [HttpPost("logo")]
    [RequestSizeLimit(2_097_152)] // 2 MB, mismo tope que el validador del comando
    public async Task<ActionResult<OrganizationDto>> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file provided.");

        await using var stream = file.OpenReadStream();
        var result = await Mediator.Send(new UploadOrganizationLogoCommand(
            Path.GetFileName(file.FileName), file.ContentType, file.Length, stream), ct);
        return Ok(result);
    }

    [HttpDelete("logo")]
    public async Task<IActionResult> DeleteLogo(CancellationToken ct)
    {
        await Mediator.Send(new DeleteOrganizationLogoCommand(), ct);
        return NoContent();
    }

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo(CancellationToken ct)
    {
        var logo = await Mediator.Send(new GetOrganizationLogoQuery(), ct);
        return logo is null ? NotFound() : File(logo.Content, logo.MimeType);
    }
}
