using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestionAlquileres.Tests.Phase5;

/// <summary>
/// Guardas del cálculo de ajuste. Todas cubren caminos que devolvían 500 o dejaban el contrato en
/// un estado del que no se podía salir.
/// </summary>
[Trait("Phase", "Phase5")]
public class AdjustmentGuardTests : IClassFixture<Phase5ApiFactory>
{
    private readonly Phase5ApiFactory _factory;
    public AdjustmentGuardTests(Phase5ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    private static async Task<Guid> CreateContractAsync(HttpClient c, string dni, object overrides)
    {
        var prop = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Alsina 900", city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        prop.EnsureSuccessStatusCode();
        var propId = (await prop.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var ten = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Marta", lastName = "Ríos", dni,
            email = (string?)null, phone = (string?)null,
        });
        ten.EnsureSuccessStatusCode();
        var tenantId = (await ten.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var body = new Dictionary<string, object?>
        {
            ["propertyId"] = propId,
            ["appTenantId"] = tenantId,
            ["startDate"] = "2026-01-01",
            ["endDate"] = "2028-01-01",
            ["monthlyRent"] = 200_000m,
            ["currency"] = "ARS",
            ["adjustmentType"] = "ICL",
            ["adjustmentFrequency"] = "Quarterly",
            ["adjustmentPercent"] = null,
            ["dayOfMonth"] = 1,
            ["depositAmount"] = null,
            ["notes"] = null,
        };
        foreach (var p in overrides.GetType().GetProperties()) body[p.Name] = p.GetValue(overrides);

        var resp = await c.PostAsJsonAsync("/api/v1/contracts", body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();
    }

    private async Task SeedIndexAsync(IndexType type, DateOnly period, decimal value)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Indexes.Add(new IndexValue { IndexType = type, Period = period, Value = value, Source = "TEST" });
        await db.SaveChangesAsync();
    }

    // Un IndexValue en 0 es persistible: la entidad no valida rango y el sincronizador copia lo que
    // devuelve la fuente. La división de abajo tiraba DivideByZeroException → 500.
    [Fact]
    public async Task Indice_base_en_cero_devuelve_error_de_negocio_y_no_un_500()
    {
        var c = await _factory.AuthedClientAsync("guard-div0");
        var contractId = await CreateContractAsync(c, "35111000", new { });

        await SeedIndexAsync(IndexType.ICL, new DateOnly(2025, 4, 1), 0m);   // período base
        await SeedIndexAsync(IndexType.ICL, new DateOnly(2026, 4, 1), 100m); // período actual

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01", manualNewRent = (decimal?)null, notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
        // El middleware serializa con escape de no-ASCII, así que se compara sobre el JSON
        // deserializado y no sobre el texto crudo.
        var body = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Contains("inválido", body.GetProperty("error").GetString()!, StringComparison.OrdinalIgnoreCase);
    }

    // Con un índice actual muy chico contra uno base grande, el importe redondea a 0. Si eso se
    // persistía en MonthlyRent, el contrato quedaba en cero y el ajuste siguiente heredaba el error.
    [Fact]
    public async Task Un_ajuste_que_da_cero_se_rechaza_en_vez_de_dejar_el_contrato_en_cero()
    {
        var c = await _factory.AuthedClientAsync("guard-cero");
        var contractId = await CreateContractAsync(c, "35111001", new { monthlyRent = 100m });

        await SeedIndexAsync(IndexType.ICL, new DateOnly(2025, 5, 1), 1_000_000m);
        await SeedIndexAsync(IndexType.ICL, new DateOnly(2026, 5, 1), 0.0001m);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-05-01", manualNewRent = (decimal?)null, notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);

        // Y el contrato conserva su alquiler.
        var get = await c.GetAsync($"/api/v1/contracts/{contractId}");
        get.EnsureSuccessStatusCode();
        var dto = await get.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(100m, dto.GetProperty("monthlyRent").GetDecimal());
    }

    // Sin effectiveDate el handler usaba DateTime.UtcNow: entre las 21:00 y las 24:00 hora
    // argentina eso ya es el día siguiente, y en el borde de fin de mes cambia el período del índice.
    [Fact]
    public async Task La_fecha_efectiva_por_defecto_usa_hora_argentina()
    {
        var c = await _factory.AuthedClientAsync("guard-tz");
        var contractId = await CreateContractAsync(c, "35111002", new
        {
            adjustmentType = "FixedPercent", adjustmentPercent = 5m,
        });

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = (string?)null, manualNewRent = (decimal?)null, notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<RentHistoryDto>(JsonOpts);
        Assert.Equal(ArgentinaTime.Today, dto!.EffectiveDate);
    }
}
