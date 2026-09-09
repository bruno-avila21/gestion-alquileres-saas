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
/// Suspender una organización tiene que cortarle el acceso.
///
/// <c>Organization.IsActive</c> existía, se seteaba al crear la organización, y no se leía en
/// NINGÚN lado: no había forma de suspender a una inmobiliaria morosa, dada de baja o cuya cuenta
/// se hubiera comprometido. Seguía operando con normalidad.
/// </summary>
public class OrganizationSuspensionTests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public OrganizationSuspensionTests(Phase7ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    private async Task SuspendAsync(Guid organizationId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await db.Organizations.FirstAsync(o => o.Id == organizationId);
        org.IsActive = false;
        await db.SaveChangesAsync();
    }

    private async Task<(Guid orgId, string email, string password)> RegisterOrgAsync(string slug)
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/auth/register-org", new
        {
            organizationName = $"{slug} Org",
            slug,
            adminEmail = $"admin@{slug}.com",
            adminPassword = "Password123!",
            adminFirstName = "Admin",
            adminLastName = "Test",
        });
        r.EnsureSuccessStatusCode();
        var auth = await r.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOpts);
        return (auth!.OrganizationId, $"admin@{slug}.com", "Password123!");
    }

    [Fact]
    public async Task Una_organizacion_suspendida_no_puede_iniciar_sesion()
    {
        const string slug = "susp-login";
        var (orgId, email, password) = await RegisterOrgAsync(slug);

        var antes = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await antes.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email, password, organizationSlug = slug,
        })).StatusCode);

        await SuspendAsync(orgId);

        var despues = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await despues.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email, password, organizationSlug = slug,
        })).StatusCode);
    }

    // Cortar el login no alcanza: quien ya estaba adentro renovaría su sesión indefinidamente.
    [Fact]
    public async Task Una_organizacion_suspendida_no_puede_renovar_la_sesion()
    {
        const string slug = "susp-refresh";
        var (orgId, email, password) = await RegisterOrgAsync(slug);

        var client = _factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email, password, organizationSlug = slug,
        })).EnsureSuccessStatusCode();

        // Con la organización activa, la renovación funciona.
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/api/v1/auth/refresh", null)).StatusCode);

        await SuspendAsync(orgId);

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsync("/api/v1/auth/refresh", null)).StatusCode);
    }

    // La verificación va DESPUÉS de validar la contraseña: hacerlo antes convertiría la respuesta
    // en un oráculo que revela qué organizaciones existen y cuáles están suspendidas, sin
    // necesidad de credenciales válidas.
    [Fact]
    public async Task Una_contrasena_incorrecta_da_el_mismo_error_este_o_no_suspendida()
    {
        const string slug = "susp-oraculo";
        var (orgId, email, _) = await RegisterOrgAsync(slug);
        await SuspendAsync(orgId);

        var c = _factory.CreateClient();
        var conClaveMal = await c.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email, password = "no-es-la-clave", organizationSlug = slug,
        });
        var conSlugInexistente = await c.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email, password = "no-es-la-clave", organizationSlug = "no-existe-esta-org",
        });

        Assert.Equal(HttpStatusCode.Unauthorized, conClaveMal.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, conSlugInexistente.StatusCode);
        Assert.Equal(
            await conSlugInexistente.Content.ReadAsStringAsync(),
            await conClaveMal.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Un_inquilino_de_una_organizacion_suspendida_tampoco_entra()
    {
        const string slug = "susp-inquilino";
        var (orgId, _, _) = await RegisterOrgAsync(slug);
        var admin = await _factory.AuthedClientAsync($"{slug}-adm");

        // El inquilino se crea en la organización del admin, no en la suspendida: se usa la propia.
        var own = _factory.CreateClient();
        var login = await own.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = $"admin@{slug}.com", password = "Password123!", organizationSlug = slug,
        });
        login.EnsureSuccessStatusCode();

        var t = await own.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Sol", lastName = "Paz", dni = "37111222",
            email = $"sol@{slug}.com", phone = (string?)null,
        });
        t.EnsureSuccessStatusCode();
        var tenantId = (await t.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var inv = await own.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        inv.EnsureSuccessStatusCode();
        var tempPassword = (await inv.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("tempPassword").GetString()!;

        await SuspendAsync(orgId);

        var tenantClient = _factory.CreateClient();
        var r = await tenantClient.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email = $"sol@{slug}.com", password = tempPassword, organizationSlug = slug,
        });

        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
        _ = admin;
    }
}
