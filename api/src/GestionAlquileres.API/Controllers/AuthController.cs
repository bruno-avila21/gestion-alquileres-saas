using GestionAlquileres.API.Configuration;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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

    /// <summary>Body fallback so non-browser API clients can refresh without a cookie jar.</summary>
    public record RefreshRequest(string? RefreshToken);

    [HttpPost("register-org")]
    public async Task<ActionResult<AuthResponseDto>> RegisterOrg(
        [FromBody] RegisterOrgCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        SetAuthCookie(result.Token);
        await IssueRefreshCookieAsync(result.UserId, result.OrganizationId, ct);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        SetAuthCookie(result.Token);
        await IssueRefreshCookieAsync(result.UserId, result.OrganizationId, ct);
        return Ok(result);
    }

    [HttpPost("tenant-login")]
    public async Task<ActionResult<AuthResponseDto>> TenantLogin(
        [FromBody] TenantLoginCommand command,
        CancellationToken ct)
    {
        var result = await Mediator.Send(command, ct);
        SetAuthCookie(result.Token);
        await IssueRefreshCookieAsync(result.UserId, result.OrganizationId, ct);
        return Ok(result);
    }

    /// <summary>
    /// Exchanges a refresh token (cookie or body) for a fresh access token, rotating the refresh token.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshRequest? body,
        CancellationToken ct)
    {
        var rawToken = Request.Cookies.TryGetValue(AuthCookie.RefreshName, out var cookie) && !string.IsNullOrEmpty(cookie)
            ? cookie
            : body?.RefreshToken;

        if (string.IsNullOrWhiteSpace(rawToken))
            return Unauthorized();

        var result = await Mediator.Send(new RefreshAccessTokenCommand(rawToken), ct);

        SetAuthCookie(result.Auth.Token);
        SetRefreshCookie(result.RawToken, result.ExpiresAt);
        return Ok(result.Auth);
    }

    /// <summary>
    /// Cambio de contraseña del usuario autenticado. Termina todas las sesiones abiertas y emite un
    /// par nuevo para ésta, así que quien hizo el cambio sigue trabajando y el resto queda afuera.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<AuthResponseDto>> ChangePassword(
        [FromBody] ChangePasswordRequest body, CancellationToken ct)
    {
        var result = await Mediator.Send(
            // El usuario sale del claim del JWT, nunca del body.
            new ChangePasswordCommand(CurrentUserId, body.CurrentPassword, body.NewPassword), ct);

        SetAuthCookie(result.Token);
        await IssueRefreshCookieAsync(result.UserId, result.OrganizationId, ct);
        return Ok(result);
    }

    public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RefreshRequest? body, CancellationToken ct)
    {
        var refresh = Request.Cookies.TryGetValue(AuthCookie.RefreshName, out var cookie) && !string.IsNullOrEmpty(cookie)
            ? cookie
            : body?.RefreshToken;
        if (!string.IsNullOrWhiteSpace(refresh))
            await Mediator.Send(new RevokeRefreshTokenCommand(refresh), ct);

        Response.Cookies.Delete(AuthCookie.Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
        });
        Response.Cookies.Delete(AuthCookie.RefreshName, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = AuthCookie.RefreshPath,
        });
        return NoContent();
    }

    private async Task IssueRefreshCookieAsync(Guid userId, Guid organizationId, CancellationToken ct)
    {
        var issued = await Mediator.Send(new IssueRefreshTokenCommand(userId, organizationId), ct);
        SetRefreshCookie(issued.RawToken, issued.ExpiresAt);
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
            MaxAge = TimeSpan.FromMinutes(_jwt.EffectiveAccessTokenMinutes),
        });
    }

    /// <summary>
    /// Stores the refresh token in an HttpOnly cookie scoped to the auth path, so it is only sent
    /// to /refresh and /logout — never on ordinary API requests.
    /// </summary>
    private void SetRefreshCookie(string rawToken, DateTimeOffset expiresAt)
    {
        Response.Cookies.Append(AuthCookie.RefreshName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = AuthCookie.RefreshPath,
            Expires = expiresAt,
        });
    }
}
