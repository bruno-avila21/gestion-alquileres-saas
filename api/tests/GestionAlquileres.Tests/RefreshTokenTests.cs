using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Auth.Commands;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GestionAlquileres.Tests;

/// <summary>
/// Backend refresh-token flow. Reuses AuthTests.ApiFactory (anonymous auth endpoints over InMemory).
/// Cookie handling is disabled so each token is driven explicitly via the request body — deterministic
/// and independent of the test HttpClient's cookie jar.
/// </summary>
public class RefreshTokenTests : IClassFixture<AuthTests.ApiFactory>
{
    private readonly AuthTests.ApiFactory _factory;
    public RefreshTokenTests(AuthTests.ApiFactory factory) => _factory = factory;

    private HttpClient NoCookieClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static string? ExtractCookie(HttpResponseMessage resp, string name)
    {
        if (!resp.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
        var c = cookies.FirstOrDefault(x => x.StartsWith(name + "=", StringComparison.Ordinal));
        if (c is null) return null;
        var firstSeg = c.Split(';')[0];        // name=value
        var eq = firstSeg.IndexOf('=');
        return eq < 0 ? null : firstSeg[(eq + 1)..];
    }

    private async Task<(HttpClient client, string refreshToken)> RegisterAsync(string slug)
    {
        var client = NoCookieClient();
        var resp = await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand($"{slug} Org", slug, $"admin@{slug}.com", "Password123!", "Admin", "Test"));
        resp.EnsureSuccessStatusCode();
        var refresh = ExtractCookie(resp, "refresh_token");
        Assert.False(string.IsNullOrWhiteSpace(refresh));
        return (client, refresh!);
    }

    [Fact]
    public async Task Login_sets_httponly_refresh_cookie_scoped_to_auth_path()
    {
        var client = NoCookieClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("RT Login Org", "rt-login", "admin@rt-login.com", "Password123!", "A", "A"));

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("admin@rt-login.com", "Password123!", "rt-login"));
        login.EnsureSuccessStatusCode();

        Assert.True(login.Headers.TryGetValues("Set-Cookie", out var cookies));
        var refreshCookie = cookies!.FirstOrDefault(c => c.StartsWith("refresh_token="));
        Assert.NotNull(refreshCookie);
        Assert.Contains("httponly", refreshCookie!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("path=/api/v1/auth", refreshCookie!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Refresh_with_valid_token_returns_new_access_token()
    {
        var (client, refresh) = await RegisterAsync("rt-valid");

        var r = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = refresh });
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var body = await r.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("rt-valid", body.OrganizationSlug);

        // A rotated refresh token is returned in the response cookie.
        Assert.NotNull(ExtractCookie(r, "refresh_token"));
    }

    [Fact]
    public async Task Refresh_with_unknown_token_returns_401()
    {
        var client = NoCookieClient();
        var r = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "not-a-real-token" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Refresh_with_no_token_returns_401()
    {
        var client = NoCookieClient();
        var r = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = (string?)null });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_token_and_old_token_is_rejected()
    {
        var (client, raw1) = await RegisterAsync("rt-rotate");

        // First refresh succeeds and yields a rotated token.
        var r1 = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = raw1 });
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var raw2 = ExtractCookie(r1, "refresh_token");
        Assert.False(string.IsNullOrWhiteSpace(raw2));
        Assert.NotEqual(raw1, raw2);

        // The new token works.
        var rNew = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = raw2 });
        Assert.Equal(HttpStatusCode.OK, rNew.StatusCode);

        // Replaying the original (already-rotated) token is rejected.
        var rOld = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = raw1 });
        Assert.Equal(HttpStatusCode.Unauthorized, rOld.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        var (client, raw) = await RegisterAsync("rt-logout");

        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken = raw });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var r = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = raw });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }
}
