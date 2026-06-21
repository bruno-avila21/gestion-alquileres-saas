using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAlquileres.Tests.Phase5;

/// <summary>
/// Regression for audit finding C-2: the ICL/IPC adjustment must be computed from the raw index
/// ratio, not from a factor pre-rounded to 6 decimals. With ICL 8/7 on a 200.000 rent the correct
/// amount is 228.571,43; the old double-rounding path produced 228.571,40.
/// </summary>
[Trait("Phase", "Phase5")]
public class IclAdjustmentPrecisionTests : IClassFixture<Phase5ApiFactory>
{
    private readonly Phase5ApiFactory _factory;

    public IclAdjustmentPrecisionTests(Phase5ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    [Fact]
    public async Task ICL_adjustment_uses_raw_index_ratio_not_prerounded_factor()
    {
        var c = await _factory.AuthedClientAsync("rent-icl-precision");

        var propResp = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Sarmiento 1200", city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        propResp.EnsureSuccessStatusCode();
        var propId = (await propResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var tenantResp = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Lucía", lastName = "Fernández", dni = "33445566",
            email = (string?)null, phone = (string?)null,
        });
        tenantResp.EnsureSuccessStatusCode();
        var tenantId = (await tenantResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var contractResp = await c.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = propId,
            appTenantId = tenantId,
            startDate = "2026-01-01",
            endDate = "2028-01-01",
            monthlyRent = 200_000m,
            currency = "ARS",
            adjustmentType = "ICL",
            adjustmentFrequency = "Quarterly",
            dayOfMonth = 1,
            depositAmount = (decimal?)null,
            notes = (string?)null,
        });
        contractResp.EnsureSuccessStatusCode();
        var contractId = (await contractResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        // Seed ICL index values directly (IndexValue is global reference data, no tenant filter).
        // ICL uses a year-over-year window (audit C-1): periodBase = 2025-04 (effective 2026-04 minus
        // 12 months), periodT = 2026-04.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Indexes.AddRange(
                new IndexValue
                {
                    IndexType = IndexType.ICL,
                    Period = new DateOnly(2025, 4, 1),
                    Value = 7m,
                    Source = "BCRA",
                },
                new IndexValue
                {
                    IndexType = IndexType.ICL,
                    Period = new DateOnly(2026, 4, 1),
                    Value = 8m,
                    Source = "BCRA",
                });
            await db.SaveChangesAsync();
        }

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01",
            manualNewRent = (decimal?)null,
            notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<RentHistoryDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(200_000m, dto!.PreviousRent);
        // 200000 * 8/7 = 228571.428... → 228571.43 (correct).
        // The pre-rounded-factor path (Round(8/7,6)=1.142857) yields 228571.40 — the bug we guard against.
        Assert.Equal(228_571.43m, dto.NewRent);
        // Factor is still persisted, rounded to 6 decimals, as informational metadata.
        Assert.Equal(1.142857m, dto.AdjustmentFactor);
    }
}
