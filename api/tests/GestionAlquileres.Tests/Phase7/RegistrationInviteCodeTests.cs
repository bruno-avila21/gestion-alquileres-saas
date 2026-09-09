using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace GestionAlquileres.Tests.Phase7;

/// <summary>Host de test con el alta de organizaciones exigiendo código de invitación.</summary>
public class InviteCodeApiFactory : Phase7ApiFactory
{
    public const string Code = "codigo-compartido-fuera-de-banda";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseSetting("Registration:Mode", "InviteCode");
        builder.UseSetting("Registration:InviteCode", Code);
    }
}

/// <summary>
/// El alta de organizaciones era anónima y sin verificación alguna: cualquiera creaba
/// organizaciones ilimitadas con emails que no controla, obtenía un JWT de Admin al instante, y
/// ocupaba slugs de marcas reales de forma IRRECUPERABLE, porque el alta los bloquea para siempre.
/// </summary>
public class RegistrationInviteCodeTests : IClassFixture<InviteCodeApiFactory>
{
    private readonly InviteCodeApiFactory _factory;
    public RegistrationInviteCodeTests(InviteCodeApiFactory factory) => _factory = factory;

    private static object Body(string slug, string? inviteCode) => new
    {
        organizationName = $"{slug} Org",
        slug,
        adminEmail = $"admin@{slug}.com",
        adminPassword = "Password123!",
        adminFirstName = "Admin",
        adminLastName = "Test",
        inviteCode,
    };

    [Fact]
    public async Task Rechaza_el_alta_sin_codigo()
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/auth/register-org", Body("inv-sin-codigo", null));

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Rechaza_el_alta_con_un_codigo_incorrecto()
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/auth/register-org", Body("inv-codigo-mal", "otro-codigo"));

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Acepta_el_alta_con_el_codigo_correcto()
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/auth/register-org",
            Body("inv-codigo-ok", InviteCodeApiFactory.Code));

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }

    // Un prefijo correcto no debe alcanzar: la comparación es en tiempo constante sobre el valor
    // completo, no un StartsWith.
    [Fact]
    public async Task Un_prefijo_del_codigo_no_alcanza()
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/auth/register-org",
            Body("inv-prefijo", InviteCodeApiFactory.Code[..10]));

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }
}
