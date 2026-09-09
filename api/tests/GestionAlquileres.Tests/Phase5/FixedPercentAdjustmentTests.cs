using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Domain.Enums;
using Xunit;

namespace GestionAlquileres.Tests.Phase5;

/// <summary>
/// Ajuste por porcentaje fijo pactado — el esquema más usado en contratos nuevos tras el
/// DNU 70/2023 ("8% trimestral"), que antes no se podía representar.
/// </summary>
[Trait("Phase", "Phase5")]
public class FixedPercentAdjustmentTests : IClassFixture<Phase5ApiFactory>
{
    private readonly Phase5ApiFactory _factory;
    public FixedPercentAdjustmentTests(Phase5ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    /// <summary>Crea propiedad, inquilino y contrato. Devuelve el id del contrato y la respuesta cruda.</summary>
    private static async Task<(HttpResponseMessage resp, Guid contractId)> CreateContractAsync(
        HttpClient c, string slug, object contractOverrides)
    {
        var propResp = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Güemes 3400", city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        propResp.EnsureSuccessStatusCode();
        var propId = (await propResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var tenantResp = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Diego", lastName = "Sosa", dni = "31222333",
            email = (string?)null, phone = (string?)null,
        });
        tenantResp.EnsureSuccessStatusCode();
        var tenantId = (await tenantResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var body = new Dictionary<string, object?>
        {
            ["propertyId"] = propId,
            ["appTenantId"] = tenantId,
            ["startDate"] = "2026-01-01",
            ["endDate"] = "2028-01-01",
            ["monthlyRent"] = 200_000m,
            ["currency"] = "ARS",
            ["adjustmentType"] = "FixedPercent",
            ["adjustmentFrequency"] = "Quarterly",
            ["adjustmentPercent"] = 8m,
            ["dayOfMonth"] = 1,
            ["depositAmount"] = null,
            ["notes"] = null,
        };
        foreach (var kv in contractOverrides.GetType().GetProperties())
            body[kv.Name] = kv.GetValue(contractOverrides);

        var resp = await c.PostAsJsonAsync("/api/v1/contracts", body);
        if (!resp.IsSuccessStatusCode) return (resp, Guid.Empty);

        var id = (await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();
        return (resp, id);
    }

    [Fact]
    public async Task Aplica_el_porcentaje_pactado_sin_depender_de_ningun_indice()
    {
        var c = await _factory.AuthedClientAsync("fixedpct-basico");
        var (_, contractId) = await CreateContractAsync(c, "fixedpct-basico", new { });

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01", manualNewRent = (decimal?)null, notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<RentHistoryDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(200_000m, dto!.PreviousRent);
        // 200.000 × 1,08 = 216.000
        Assert.Equal(216_000m, dto.NewRent);
        Assert.Equal(1.08m, dto.AdjustmentFactor);
        Assert.Equal(AdjustmentType.FixedPercent, dto.AdjustmentType);
        // No hay índice involucrado: el ajuste no queda atado a ningún IndexValue.
        Assert.Null(dto.IndexValueId);
    }

    [Fact]
    public async Task El_porcentaje_compone_sobre_el_alquiler_ya_ajustado()
    {
        var c = await _factory.AuthedClientAsync("fixedpct-compone");
        var (_, contractId) = await CreateContractAsync(c, "fixedpct-compone", new { });

        await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01", manualNewRent = (decimal?)null, notes = (string?)null,
        });
        var r2 = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-07-01", manualNewRent = (decimal?)null, notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var dto = await r2.Content.ReadFromJsonAsync<RentHistoryDto>(JsonOpts);
        // Segundo escalón: 216.000 × 1,08 = 233.280 — compone, no se aplica sobre el original.
        Assert.Equal(216_000m, dto!.PreviousRent);
        Assert.Equal(233_280m, dto.NewRent);
    }

    // El redondeo se hace sobre el importe final, no sobre el factor: con un porcentaje de tres
    // decimales el error de pre-redondear se vuelve visible y se acumularía en cada escalón.
    [Fact]
    public async Task Redondea_el_importe_y_no_el_factor()
    {
        var c = await _factory.AuthedClientAsync("fixedpct-redondeo");
        var (_, contractId) = await CreateContractAsync(
            c, "fixedpct-redondeo", new { monthlyRent = 333_333m, adjustmentPercent = 7.125m });

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01", manualNewRent = (decimal?)null, notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<RentHistoryDto>(JsonOpts);
        // 333.333 × 1,07125 = 357.082,97625 → 357.082,98
        Assert.Equal(357_082.98m, dto!.NewRent);
    }

    [Fact]
    public async Task Rechaza_el_alta_sin_porcentaje_cuando_el_tipo_es_porcentaje_fijo()
    {
        var c = await _factory.AuthedClientAsync("fixedpct-sin-pct");
        var (resp, _) = await CreateContractAsync(
            c, "fixedpct-sin-pct", new { adjustmentPercent = (decimal?)null });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var body = await resp.Content.ReadAsStringAsync();
        Assert.Contains("porcentaje", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rechaza_un_porcentaje_en_un_contrato_que_no_es_de_porcentaje_fijo()
    {
        var c = await _factory.AuthedClientAsync("fixedpct-sobrante");
        var (resp, _) = await CreateContractAsync(
            c, "fixedpct-sobrante", new { adjustmentType = "ICL", adjustmentPercent = 8m });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    // Antes de agregar IsInEnum, un tipo inválido se persistía como entero crudo y el motor lo
    // trataba como IPC: el contrato terminaba ajustado con el índice equivocado.
    [Fact]
    public async Task Rechaza_un_tipo_de_ajuste_invalido()
    {
        var c = await _factory.AuthedClientAsync("fixedpct-enum");
        var (resp, _) = await CreateContractAsync(
            c, "fixedpct-enum", new { adjustmentType = 99, adjustmentPercent = (decimal?)null });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Theory]
    [InlineData("FourMonthly")]
    [InlineData("SemiAnnual")]
    public async Task Acepta_las_frecuencias_nuevas(string frequency)
    {
        var c = await _factory.AuthedClientAsync($"freq-{frequency.ToLowerInvariant()}");
        var (resp, contractId) = await CreateContractAsync(
            c, "freq", new { adjustmentFrequency = frequency });

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var get = await c.GetAsync($"/api/v1/contracts/{contractId}");
        get.EnsureSuccessStatusCode();
        var dto = await get.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(frequency, dto.GetProperty("adjustmentFrequency").GetString());
    }

    [Fact]
    public async Task Proyecta_el_escalonado_sin_llamar_a_indices_api()
    {
        var c = await _factory.AuthedClientAsync("fixedpct-proyeccion");
        var (_, contractId) = await CreateContractAsync(c, "fixedpct-proyeccion", new { });

        var r = await c.GetAsync($"/api/v1/contracts/{contractId}/adjustment-projection");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var proj = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var schedule = proj.GetProperty("schedule");

        // Contrato de 2 años con ajuste trimestral → 8 períodos.
        Assert.Equal(8, schedule.GetArrayLength());
        // El primer período corre al alquiler original.
        Assert.Equal(200_000m, schedule[0].GetProperty("rent").GetDecimal());
        // El segundo ya lleva el escalón.
        Assert.Equal(216_000m, schedule[1].GetProperty("rent").GetDecimal());
        // Y el tercero compone.
        Assert.Equal(233_280m, schedule[2].GetProperty("rent").GetDecimal());
    }
}
