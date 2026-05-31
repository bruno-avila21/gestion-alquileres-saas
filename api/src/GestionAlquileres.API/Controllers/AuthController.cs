using GestionAlquileres.API.Configuration;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace GestionAlquileres.API.Controllers;

[EnableRateLimiting("auth")]
[AllowAnonymous]
[Route("api/v1/auth")]
public class AuthController : BaseController
{
    private readonly JwtSettings _jwt;

    public AuthController(IOptions<JwtSettings> jwt) => _jwt = jwt.Value;

    [HttpPost("register-org")]
    public async Task<ActionResult<AuthResponseDto>> RegisterOrg(
        [FromBody] RegisterOrgCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        SetAuthCookie(result.Token);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        SetAuthCookie(result.Token);
        return Ok(result);
    }

    [HttpPost("tenant-login")]
    public async Task<ActionResult<AuthResponseDto>> TenantLogin(
        [FromBody] TenantLoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        SetAuthCookie(result.Token);
        return Ok(result);
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie.Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
        return NoContent();
    }

    /// <summary>
    /// Stores the JWT in an HttpOnly cookie so the browser sends it automatically and
    /// JavaScript cannot read it (mitigates token theft via XSS). The token is still
    /// returned in the body for non-browser API clients.
    /// </summary>
    private void SetAuthCookie(string token)
    {
        Response.Cookies.Append(AuthCookie.Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,        // sent over HTTPS in prod; allows http://localhost in dev
            SameSite = SameSiteMode.Lax,     // cross-site deployments should override to None + Secure
            Path = "/",
            MaxAge = TimeSpan.FromHours(_jwt.ExpiryHours),
        });
    }
}
