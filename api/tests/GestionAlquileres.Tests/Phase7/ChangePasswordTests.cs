using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestionAlquileres.Tests.Phase7;

/// <summary>
/// Cambio de contraseña y re-invitación.
///
/// Antes no existía ninguna vía para rotar una credencial: la contraseña temporal que el sistema
/// generaba para el inquilino, y que el administrador le pasaba por WhatsApp, era su credencial
/// definitiva. Ni el inquilino ni el administrador podían cambiarla — re-invitar fallaba con
/// "ya tiene acceso al portal" — así que ante una filtración había que tocar la base a mano.
/// </summary>
public class ChangePasswordTests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public ChangePasswordTests(Phase7ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    private static async Task<(Guid tenantId, string email, string password)> InviteTenantAsync(
        HttpClient admin, string slug, string dni = "34111222")
    {
        var t = await admin.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Pablo", lastName = "Vega", dni,
            email = $"pablo@{slug}.com", phone = (string?)null,
        });
        t.EnsureSuccessStatusCode();
        var tenantId = (await t.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var inv = await admin.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        inv.EnsureSuccessStatusCode();
        var password = (await inv.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("tempPassword").GetString()!;

        return (tenantId, $"pablo@{slug}.com", password);
    }

    private async Task<(HttpClient client, AuthResponseDto auth)> TenantLoginAsync(
        string slug, string email, string password)
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/auth/tenant-login", new { email, password, organizationSlug = slug });
        r.EnsureSuccessStatusCode();
        var auth = await r.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts);
        return (c, auth!);
    }

    // La contraseña generada por el sistema viaja fuera de banda y queda en el historial de esa
    // conversación: no puede ser la credencial definitiva.
    [Fact]
    public async Task La_invitacion_marca_que_hay_que_cambiar_la_contrasena()
    {
        const string slug = "pwd-forzado";
        var admin = await _factory.AuthedClientAsync(slug);
        var (_, email, password) = await InviteTenantAsync(admin, slug);

        var (_, auth) = await TenantLoginAsync(slug, email, password);

        Assert.True(auth.MustChangePassword);
    }

    [Fact]
    public async Task Cambiar_la_contrasena_permite_entrar_con_la_nueva_y_no_con_la_vieja()
    {
        const string slug = "pwd-cambio";
        var admin = await _factory.AuthedClientAsync(slug);
        var (_, email, oldPassword) = await InviteTenantAsync(admin, slug);
        var (client, _) = await TenantLoginAsync(slug, email, oldPassword);

        const string newPassword = "una-contrasena-larga-2026";
        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = oldPassword, newPassword,
        });

        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var auth = await change.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts);
        Assert.False(auth!.MustChangePassword);

        var conNueva = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await conNueva.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email, password = newPassword, organizationSlug = slug,
        })).StatusCode);

        var conVieja = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await conVieja.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email, password = oldPassword, organizationSlug = slug,
        })).StatusCode);
    }

    [Fact]
    public async Task Rechaza_el_cambio_si_la_contrasena_actual_es_incorrecta()
    {
        const string slug = "pwd-actual-mal";
        var admin = await _factory.AuthedClientAsync(slug);
        var (_, email, password) = await InviteTenantAsync(admin, slug);
        var (client, _) = await TenantLoginAsync(slug, email, password);

        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "esta-no-es-la-actual", newPassword = "una-contrasena-larga-2026",
        });

        Assert.Equal(HttpStatusCode.Conflict, change.StatusCode);
    }

    [Fact]
    public async Task Rechaza_una_contrasena_nueva_demasiado_corta()
    {
        const string slug = "pwd-corta";
        var admin = await _factory.AuthedClientAsync(slug);
        var (_, email, password) = await InviteTenantAsync(admin, slug);
        var (client, _) = await TenantLoginAsync(slug, email, password);

        var change = await client.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = password, newPassword = "corta",
        });

        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
    }

    [Fact]
    public async Task El_endpoint_exige_autenticacion()
    {
        var anon = _factory.CreateClient();
        var r = await anon.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = "x", newPassword = "una-contrasena-larga-2026",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // Si el motivo del cambio es que la credencial se filtró, dejar vivas las sesiones abiertas
    // con la contraseña vieja no sirve de nada.
    [Fact]
    public async Task Cambiar_la_contrasena_revoca_las_sesiones_abiertas()
    {
        const string slug = "pwd-revoca";
        var admin = await _factory.AuthedClientAsync(slug);
        var (_, email, password) = await InviteTenantAsync(admin, slug);

        // Dos dispositivos con sesión iniciada.
        var (device1, auth) = await TenantLoginAsync(slug, email, password);
        await TenantLoginAsync(slug, email, password);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            Assert.Equal(2, await db.Set<RefreshToken>()
                .CountAsync(t => t.UserId == auth.UserId && t.RevokedAt == null));
        }

        (await device1.PostAsJsonAsync("/api/v1/auth/change-password", new
        {
            currentPassword = password, newPassword = "una-contrasena-larga-2026",
        })).EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Queda vivo sólo el par nuevo que se emitió para la sesión que hizo el cambio.
            Assert.Equal(1, await db.Set<RefreshToken>()
                .CountAsync(t => t.UserId == auth.UserId && t.RevokedAt == null));
        }
    }

    // Antes lanzaba "Este inquilino ya tiene acceso al portal", con lo cual una contraseña perdida
    // no se podía rotar por ninguna vía.
    [Fact]
    public async Task Re_invitar_regenera_la_contrasena_y_deja_la_anterior_sin_efecto()
    {
        const string slug = "pwd-reinvitar";
        var admin = await _factory.AuthedClientAsync(slug);
        var (tenantId, email, primera) = await InviteTenantAsync(admin, slug);

        var segundaResp = await admin.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        Assert.Equal(HttpStatusCode.OK, segundaResp.StatusCode);
        var segunda = (await segundaResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("tempPassword").GetString()!;

        Assert.NotEqual(primera, segunda);

        var conSegunda = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await conSegunda.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email, password = segunda, organizationSlug = slug,
        })).StatusCode);

        var conPrimera = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await conPrimera.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email, password = primera, organizationSlug = slug,
        })).StatusCode);
    }

    // Cierra la asimetría que quedó abierta al implementar la revocación: reactivar desde la
    // edición no devuelve el acceso, pero re-invitar sí — y es explícito.
    [Fact]
    public async Task Re_invitar_le_devuelve_el_acceso_a_un_inquilino_dado_de_baja()
    {
        const string slug = "pwd-rehabilita";
        var admin = await _factory.AuthedClientAsync(slug);
        var (tenantId, email, _) = await InviteTenantAsync(admin, slug);

        (await admin.DeleteAsync($"/api/v1/tenants/{tenantId}")).EnsureSuccessStatusCode();

        var reinvite = await admin.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        reinvite.EnsureSuccessStatusCode();
        var nueva = (await reinvite.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("tempPassword").GetString()!;

        var c = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await c.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email, password = nueva, organizationSlug = slug,
        })).StatusCode);
    }
}
