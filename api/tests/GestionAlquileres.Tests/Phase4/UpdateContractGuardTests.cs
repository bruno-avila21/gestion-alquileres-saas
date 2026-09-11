using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace GestionAlquileres.Tests.Phase4;

/// <summary>
/// La edición de contrato tiene que verificar que la propiedad y el inquilino pertenezcan a la
/// organización, igual que el alta.
///
/// Sin eso se podía asignar la propiedad de OTRA organización: la clave foránea es global, así que
/// la fila se escribía sin error. El contrato quedaba corrupto en silencio y, como los listados
/// org-wide usan inner join, sus transacciones y ajustes desaparecían de las pantallas de Pagos y
/// Ajustes sin ningún mensaje.
/// </summary>
[Trait("Phase", "Phase4")]
public class UpdateContractGuardTests : IClassFixture<Phase4ApiFactory>
{
    private readonly Phase4ApiFactory _factory;
    public UpdateContractGuardTests(Phase4ApiFactory factory) => _factory = factory;

    private static async Task<(Guid propertyId, Guid tenantId)> SeedPartiesAsync(
        HttpClient c, string dni, string address)
    {
        var p = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address, city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        p.EnsureSuccessStatusCode();
        var propertyId = (await p.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var t = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Ivan", lastName = "Moro", dni,
            email = (string?)null, phone = (string?)null,
        });
        t.EnsureSuccessStatusCode();
        var tenantId = (await t.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        return (propertyId, tenantId);
    }

    private static object ContractBody(Guid propertyId, Guid tenantId) => new
    {
        propertyId,
        appTenantId = tenantId,
        startDate = "2026-01-01",
        endDate = "2028-01-01",
        monthlyRent = 250_000m,
        currency = "ARS",
        adjustmentType = "Manual",
        adjustmentFrequency = "Quarterly",
        adjustmentPercent = (decimal?)null,
        dayOfMonth = 1,
        depositAmount = (decimal?)null,
        notes = (string?)null,
    };

    [Fact]
    public async Task Rechaza_asignar_una_propiedad_de_otra_organizacion()
    {
        var orgA = await _factory.AuthedClientAsync("upd-guard-a");
        var orgB = await _factory.AuthedClientAsync("upd-guard-b");

        var (propA, tenA) = await SeedPartiesAsync(orgA, "36111000", "Lavalle 100");
        var (propB, _) = await SeedPartiesAsync(orgB, "36111001", "Maipú 200");

        var create = await orgA.PostAsJsonAsync("/api/v1/contracts", ContractBody(propA, tenA));
        create.EnsureSuccessStatusCode();
        var contractId = (await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        // Org A intenta apuntar su contrato a una propiedad de Org B.
        var upd = await orgA.PutAsJsonAsync($"/api/v1/contracts/{contractId}", ContractBody(propB, tenA));

        Assert.Equal(HttpStatusCode.Conflict, upd.StatusCode);

        // Y el contrato conserva su propiedad original.
        var get = await orgA.GetAsync($"/api/v1/contracts/{contractId}");
        get.EnsureSuccessStatusCode();
        var dto = await get.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(propA, dto.GetProperty("propertyId").GetGuid());
    }

    [Fact]
    public async Task Rechaza_asignar_un_inquilino_de_otra_organizacion()
    {
        var orgA = await _factory.AuthedClientAsync("upd-guard-c");
        var orgB = await _factory.AuthedClientAsync("upd-guard-d");

        var (propA, tenA) = await SeedPartiesAsync(orgA, "36111002", "Uriburu 300");
        var (_, tenB) = await SeedPartiesAsync(orgB, "36111003", "Junín 400");

        var create = await orgA.PostAsJsonAsync("/api/v1/contracts", ContractBody(propA, tenA));
        create.EnsureSuccessStatusCode();
        var contractId = (await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var upd = await orgA.PutAsJsonAsync($"/api/v1/contracts/{contractId}", ContractBody(propA, tenB));

        Assert.Equal(HttpStatusCode.Conflict, upd.StatusCode);
    }

    [Fact]
    public async Task Acepta_una_edicion_valida_dentro_de_la_organizacion()
    {
        var org = await _factory.AuthedClientAsync("upd-guard-ok");
        var (prop, ten) = await SeedPartiesAsync(org, "36111004", "Bulnes 500");

        var create = await org.PostAsJsonAsync("/api/v1/contracts", ContractBody(prop, ten));
        create.EnsureSuccessStatusCode();
        var contractId = (await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var body = new Dictionary<string, object?>();
        foreach (var p in ContractBody(prop, ten).GetType().GetProperties())
            body[p.Name] = p.GetValue(ContractBody(prop, ten));
        body["monthlyRent"] = 300_000m;

        var upd = await org.PutAsJsonAsync($"/api/v1/contracts/{contractId}", body);

        Assert.Equal(HttpStatusCode.OK, upd.StatusCode);
        var dto = await upd.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(300_000m, dto.GetProperty("monthlyRent").GetDecimal());
    }

    [Fact]
    public async Task Rechaza_editar_un_contrato_rescindido()
    {
        var org = await _factory.AuthedClientAsync("upd-guard-term");
        var (prop, ten) = await SeedPartiesAsync(org, "36111005", "Salguero 600");

        var create = await org.PostAsJsonAsync("/api/v1/contracts", ContractBody(prop, ten));
        create.EnsureSuccessStatusCode();
        var contractId = (await create.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        (await org.PostAsJsonAsync($"/api/v1/contracts/{contractId}/terminate", new
        {
            notes = "fin de la locación",
        })).EnsureSuccessStatusCode();

        var upd = await org.PutAsJsonAsync($"/api/v1/contracts/{contractId}", ContractBody(prop, ten));

        Assert.Equal(HttpStatusCode.Conflict, upd.StatusCode);
    }
}
