using GestionAlquileres.Application.Features.PropertyPhotos.Commands;
using GestionAlquileres.Application.Features.PropertyPhotos.DTOs;
using GestionAlquileres.Application.Features.PropertyPhotos.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

/// <summary>Fotos de la ficha pública de una propiedad.</summary>
[Route("api/v1/properties/{propertyId:guid}/photos")]
public class PropertyPhotosController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertyPhotoDto>>> GetAll(Guid propertyId, CancellationToken ct) =>
        Ok(await Mediator.Send(new GetPropertyPhotosQuery(propertyId), ct));

    [HttpPost]
    [RequestSizeLimit(10_485_760)] // 10 MB, same cap as the validator
    public async Task<ActionResult<PropertyPhotoDto>> Upload(Guid propertyId, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0) return BadRequest("No file provided.");

        await using var stream = file.OpenReadStream();
        var dto = await Mediator.Send(new UploadPropertyPhotoCommand(
            propertyId, Path.GetFileName(file.FileName), file.ContentType, file.Length, stream), ct);
        return CreatedAtAction(nameof(GetAll), new { propertyId }, dto);
    }

    [HttpPut("{photoId:guid}/cover")]
    public async Task<IActionResult> SetCover(Guid propertyId, Guid photoId, CancellationToken ct)
    {
        await Mediator.Send(new SetCoverPhotoCommand(propertyId, photoId), ct);
        return NoContent();
    }

    [HttpDelete("{photoId:guid}")]
    public async Task<IActionResult> Delete(Guid propertyId, Guid photoId, CancellationToken ct)
    {
        await Mediator.Send(new DeletePropertyPhotoCommand(propertyId, photoId), ct);
        return NoContent();
    }
}
