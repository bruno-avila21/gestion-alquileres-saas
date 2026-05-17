using GestionAlquileres.API.Contracts;
using GestionAlquileres.Application.Features.AppTenants.Commands;
using GestionAlquileres.Application.Features.AppTenants.DTOs;
using GestionAlquileres.Application.Features.AppTenants.Queries;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[Route("api/v1/tenants")]
public class AppTenantsController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppTenantDto>>> GetAll(CancellationToken ct) =>
        Ok(await Mediator.Send(new GetAppTenantsQuery(), ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AppTenantDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetAppTenantByIdQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AppTenantDto>> Create(
        [FromBody] CreateAppTenantRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateAppTenantCommand(
            OrganizationId, request.FirstName, request.LastName,
            request.Dni, request.Email, request.Phone), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AppTenantDto>> Update(
        Guid id, [FromBody] UpdateAppTenantRequest request, CancellationToken ct)
    {
        var result = await Mediator.Send(new UpdateAppTenantCommand(
            id, request.FirstName, request.LastName, request.Dni,
            request.Email, request.Phone, request.IsActive), ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteAppTenantCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/invite")]
    public async Task<ActionResult<InviteTenantResult>> Invite(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new InviteTenantCommand(id), ct);
        return Ok(result);
    }
}
