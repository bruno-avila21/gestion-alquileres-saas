using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestionAlquileres.Tests.Phase7;

/// <summary>
/// Dar de baja o desactivar a un inquilino tiene que cortarle el acceso al portal.
///
/// Antes sólo se ponía AppTenant.IsActive = false, y como el login del portal valida
/// User.IsActive —no el del inquilino— un ex-inquilino seguía entrando indefinidamente y viendo su
/// contrato, sus pagos y sus documentos.
/// </summary>
public class TenantRevocationTests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public TenantRevocationTests(Phase7ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    /// <summary>Crea un inquilino con acceso al portal y devuelve su id, email y contraseña temporal.</summary>
    private static async Task<(Guid tenantId, string email, string password)> InviteTenantAsync(
        HttpClient admin, string slug)
    {
        var tenantResp = await admin.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Nadia", lastName = "Ruiz", dni = "32999888",
            email = $"nadia@{slug}.com", phone = (string?)null,
        });
        tenantResp.EnsureSuccessStatusCode();
        var tenantId = (await tenantResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var inviteResp = await admin.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        inviteResp.EnsureSuccessStatusCode();
        var invite = await inviteResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var password = invite.GetProperty("tempPassword").GetString()!;

        return (tenantId, $"nadia@{slug}.com", password);
    }

    private async Task<HttpResponseMessage> TenantLoginAsync(string slug, string email, string password)
    {
        var c = _factory.CreateClient();
        return await c.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email, password, organizationSlug = slug,
        });
    }

    [Fact]
    public async Task Dar_de_baja_a_un_inquilino_le_corta_el_login()
    {
        const string slug = "revoke-delete";
        var admin = await _factory.AuthedClientAsync(slug);
        var (tenantId, email, password) = await InviteTenantAsync(admin, slug);

        // Antes de la baja entra sin problema.
        Assert.Equal(HttpStatusCode.OK, (await TenantLoginAsync(slug, email, password)).StatusCode);

        var del = await admin.DeleteAsync($"/api/v1/tenants/{tenantId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        // Después de la baja, no.
        Assert.Equal(HttpStatusCode.Unauthorized, (await TenantLoginAsync(slug, email, password)).StatusCode);
    }

    [Fact]
    public async Task Desactivar_a_un_inquilino_desde_la_edicion_tambien_le_corta_el_login()
    {
        const string slug = "revoke-update";
        var admin = await _factory.AuthedClientAsync(slug);
        var (tenantId, email, password) = await InviteTenantAsync(admin, slug);

        Assert.Equal(HttpStatusCode.OK, (await TenantLoginAsync(slug, email, password)).StatusCode);

        var upd = await admin.PutAsJsonAsync($"/api/v1/tenants/{tenantId}", new
        {
            firstName = "Nadia", lastName = "Ruiz", dni = "32999888",
            email, phone = (string?)null, isActive = false,
        });
        upd.EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await TenantLoginAsync(slug, email, password)).StatusCode);
    }

    // Cortar el login no alcanza: si le quedan refresh tokens vivos, el ex-inquilino renueva su
    // sesión indefinidamente sin volver a pasar por la contraseña.
    [Fact]
    public async Task La_baja_revoca_los_refresh_tokens_vivos()
    {
        const string slug = "revoke-refresh";
        var admin = await _factory.AuthedClientAsync(slug);
        var (tenantId, email, password) = await InviteTenantAsync(admin, slug);

        var login = await TenantLoginAsync(slug, email, password);
        login.EnsureSuccessStatusCode();
        var auth = await login.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts);
        var userId = auth!.UserId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var vivos = await db.Set<RefreshToken>()
                .CountAsync(t => t.UserId == userId && t.RevokedAt == null);
            Assert.True(vivos > 0, "el login debería haber emitido un refresh token");
        }

        (await admin.DeleteAsync($"/api/v1/tenants/{tenantId}")).EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var vivos = await db.Set<RefreshToken>()
                .CountAsync(t => t.UserId == userId && t.RevokedAt == null);
            Assert.Equal(0, vivos);
        }
    }

    [Fact]
    public async Task Reactivar_a_un_inquilino_no_le_devuelve_el_acceso_automaticamente()
    {
        const string slug = "revoke-reactivar";
        var admin = await _factory.AuthedClientAsync(slug);
        var (tenantId, email, password) = await InviteTenantAsync(admin, slug);

        (await admin.DeleteAsync($"/api/v1/tenants/{tenantId}")).EnsureSuccessStatusCode();

        var upd = await admin.PutAsJsonAsync($"/api/v1/tenants/{tenantId}", new
        {
            firstName = "Nadia", lastName = "Ruiz", dni = "32999888",
            email, phone = (string?)null, isActive = true,
        });
        upd.EnsureSuccessStatusCode();

        // Documenta el comportamiento actual: reactivar el inquilino NO reactiva su usuario, así
        // que sigue sin poder entrar. Es el lado conservador del error, pero es una asimetría real
        // que hay que resolver con producto (hoy la interfaz no avisa).
        Assert.Equal(HttpStatusCode.Unauthorized, (await TenantLoginAsync(slug, email, password)).StatusCode);
    }
}
