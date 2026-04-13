using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionAlquileres.API.Controllers;

[AllowAnonymous]
[Route("api/v1/auth")]
public class AuthController : BaseController
{
    [HttpPost("register-org")]
    public async Task<ActionResult<AuthResponseDto>> RegisterOrg(
        [FromBody] RegisterOrgCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("tenant-login")]
    public async Task<ActionResult<AuthResponseDto>> TenantLogin(
        [FromBody] TenantLoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        return Ok(result);
    }
}
