using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.SiteSettings.Commands;
using GestionAlquileres.Application.Features.SiteSettings.DTOs;
using GestionAlquileres.Application.Features.SiteSettings.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

/// <summary>Aspecto y textos del sitio público. La lectura anónima va por PublicController.</summary>
[Route("api/v1/site-settings")]
public class SiteSettingsController : AdminControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SiteSettingsDto>> Get(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetSiteSettingsQuery(), ct));

    [HttpPut]
    public async Task<ActionResult<SiteSettingsDto>> Update(
        [FromBody] UpdateSiteSettingsRequest request, CancellationToken ct) =>
        Ok(await Mediator.Send(new UpdateSiteSettingsCommand(
            request.AccentColor, request.FontPairing, request.HeroTitle,
            request.HeroSubtitle, request.AboutText, request.FooterTagline), ct));
}
