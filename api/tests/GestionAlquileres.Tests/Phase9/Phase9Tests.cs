using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Tests.Phase7;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestionAlquileres.Tests.Phase9;

/// <summary>
/// Phase 9 integration tests:
///   GET /api/v1/rent-adjustments/export
///   Background-job raw repository methods (cross-tenant visibility)
/// </summary>
[Trait("Phase", "Phase9")]
public class Phase9Tests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public Phase9Tests(Phase7ApiFactory factory) => _factory = factory;

    // ── GET /api/v1/rent-adjustments/export ─────────────────────────────────

    [Fact]
    public async Task T1_AdjustExport_NoAuth_Returns401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/rent-adjustments/export");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T2_AdjustExport_WithData_ReturnsCsvFile()
    {
        var (contractId, client) = await _factory.SetupContractAsync("adjexp-t2");

        // Apply a manual adjustment so there is data to export
        (await client.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-07-01",
            manualNewRent = 310_000m,
            notes = "Ajuste acuerdo julio",
        })).EnsureSuccessStatusCode();

        var r = await client.GetAsync("/api/v1/rent-adjustments/export");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("text/csv", r.Content.Headers.ContentType?.MediaType ?? "");

        var csv = await r.Content.ReadAsStringAsync();
        Assert.Contains("Id,ContractId,TipoAjuste", csv);  // header row
        Assert.Contains("Manual", csv);                     // adjustment type
        Assert.Contains("310000", csv);                     // new rent value
        Assert.Contains("250000", csv);                     // previous rent value
    }

    [Fact]
    public async Task T3_AdjustExport_EmptyOrg_ReturnsCsvHeaderOnly()
    {
        var client = await _factory.AuthedClientAsync("adjexp-t3");

        var r = await client.GetAsync("/api/v1/rent-adjustments/export");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var csv = await r.Content.ReadAsStringAsync();
        var lines = csv.Trim().Split('\n');
        Assert.Single(lines); // only the header, no data rows
    }

    // ── Background-job raw repository methods ────────────────────────────────
    // These tests verify that the Raw methods (used by Hangfire jobs) bypass
    // the EF Core global query filter and can see contracts across all orgs.

    [Fact]
    public async Task T4_RawRepo_ListActiveRawAsync_SeesAllOrgsContracts()
    {
        // Create 2 separate orgs, each with an active contract
        var (contractIdA, _) = await _factory.SetupContractAsync("raw-t4a");
        var (contractIdB, _) = await _factory.SetupContractAsync("raw-t4b");

        using var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IContractRepository>();

        var all = await repo.ListActiveRawAsync(CancellationToken.None);

        // Both contracts from different orgs must appear — this would fail
        // before the fix because ListAsync filtered by OrganizationId == Guid.Empty
        Assert.Contains(all, c => c.Id == contractIdA);
        Assert.Contains(all, c => c.Id == contractIdB);
    }

    [Fact]
    public async Task T5_RawRepo_GetLastByContractRawAsync_FindsHistoryAcrossOrgs()
    {
        var (contractId, client) = await _factory.SetupContractAsync("raw-t5");

        // Apply an adjustment so RentHistory has a record
        (await client.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-08-01",
            manualNewRent = 275_000m,
            notes = "Ajuste raw test",
        })).EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var histRepo = scope.ServiceProvider.GetRequiredService<IRentHistoryRepository>();

        // GetLastByContractRawAsync must find the record regardless of current tenant context
        var last = await histRepo.GetLastByContractRawAsync(contractId, CancellationToken.None);

        Assert.NotNull(last);
        Assert.Equal(contractId, last.ContractId);
        Assert.Equal(275_000m, last.NewRent);
    }
}
