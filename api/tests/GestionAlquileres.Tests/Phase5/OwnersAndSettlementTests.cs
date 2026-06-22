using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.Owners.DTOs;
using GestionAlquileres.Application.Features.Properties.DTOs;

namespace GestionAlquileres.Tests.Phase5;

/// <summary>Audit M-3: owner CRUD, property↔owner link with commission, and the on-demand settlement.</summary>
[Trait("Phase", "Phase5")]
public class OwnersAndSettlementTests : IClassFixture<Phase5ApiFactory>
{
    private readonly Phase5ApiFactory _factory;

    public OwnersAndSettlementTests(Phase5ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    private static async Task<Guid> CreateOwnerAsync(HttpClient c, string name = "Juan Propietario", decimal? _ = null)
    {
        var r = await c.PostAsJsonAsync("/api/v1/owners", new
        {
            name, taxId = "20304050607", email = "juan@example.com", phone = (string?)null, cbu = (string?)null, notes = (string?)null,
        });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<OwnerDto>(JsonOpts))!.Id;
    }

    private static async Task<Guid> CreatePropertyAsync(HttpClient c, Guid ownerId, decimal commissionPct)
    {
        var r = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Belgrano 1500", city = "CABA", province = "CABA", propertyType = "Apartment",
            areaM2 = (decimal?)null, notes = (string?)null, ownerId, commissionPct,
        });
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts))!.Id;
    }

    [Fact]
    public async Task Create_and_get_owner()
    {
        var c = await _factory.AuthedClientAsync("own-crud");
        var ownerId = await CreateOwnerAsync(c, "Marta Dueña");

        var r = await c.GetAsync($"/api/v1/owners/{ownerId}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<OwnerDto>(JsonOpts);
        Assert.Equal("Marta Dueña", dto!.Name);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task Property_links_to_owner_with_commission()
    {
        var c = await _factory.AuthedClientAsync("own-link");
        var ownerId = await CreateOwnerAsync(c);
        var propId = await CreatePropertyAsync(c, ownerId, 8.5m);

        var r = await c.GetAsync($"/api/v1/properties/{propId}");
        var dto = await r.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);
        Assert.Equal(ownerId, dto!.OwnerId);
        Assert.Equal(8.5m, dto.CommissionPct);
    }

    [Fact]
    public async Task Property_with_unknown_owner_returns_409()
    {
        var c = await _factory.AuthedClientAsync("own-bad");
        var r = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "X", city = "Y", province = "Z", propertyType = "Apartment",
            areaM2 = (decimal?)null, notes = (string?)null, ownerId = Guid.NewGuid(), commissionPct = (decimal?)null,
        });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task Commission_over_100_returns_400()
    {
        var c = await _factory.AuthedClientAsync("own-commission");
        var ownerId = await CreateOwnerAsync(c);
        var r = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "X", city = "Y", province = "Z", propertyType = "Apartment",
            areaM2 = (decimal?)null, notes = (string?)null, ownerId, commissionPct = 150m,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Settlement_nets_commission_off_collected_payments()
    {
        var c = await _factory.AuthedClientAsync("own-settle");
        var ownerId = await CreateOwnerAsync(c);
        var propId = await CreatePropertyAsync(c, ownerId, 10m); // 10% commission

        var tenantR = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Inq", lastName = "Uilino", dni = "12121212", email = (string?)null, phone = (string?)null,
        });
        tenantR.EnsureSuccessStatusCode();
        var tenantId = (await tenantR.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();

        var contractR = await c.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = propId, appTenantId = tenantId, startDate = "2026-01-01", endDate = "2028-01-01",
            monthlyRent = 100_000m, currency = "ARS", adjustmentType = "Manual", adjustmentFrequency = "Quarterly",
            dayOfMonth = 1, depositAmount = (decimal?)null, notes = (string?)null,
        });
        contractR.EnsureSuccessStatusCode();
        var contractId = (await contractR.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();

        // Two payments in March 2026 totalling 100.000
        await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new { amount = 60_000m, period = "2026-03-01", notes = (string?)null });
        await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new { amount = 40_000m, period = "2026-03-01", notes = (string?)null });
        // A payment outside the period must be excluded
        await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new { amount = 99_000m, period = "2026-04-01", notes = (string?)null });

        var r = await c.GetAsync($"/api/v1/owners/{ownerId}/settlement?from=2026-03-01&to=2026-03-01");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var s = await r.Content.ReadFromJsonAsync<OwnerSettlementDto>(JsonOpts);

        Assert.Equal(100_000m, s!.GrossCollected);
        Assert.Equal(10_000m, s.CommissionAmount); // 10% of 100k
        Assert.Equal(90_000m, s.NetToOwner);
        Assert.Single(s.Lines);
        Assert.Equal(contractId, s.Lines[0].ContractId);
    }
}
