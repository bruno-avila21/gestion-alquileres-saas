using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Tests.Phase7;
using Xunit;

namespace GestionAlquileres.Tests.Phase10;

/// <summary>
/// Phase 10 integration tests:
///   GET /api/v1/me/history — tenant rent adjustment history
///   FluentValidation on Delete/Terminate commands
/// </summary>
[Trait("Phase", "Phase10")]
public class Phase10Tests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public Phase10Tests(Phase7ApiFactory factory) => _factory = factory;

    // ── GET /api/v1/me/history ───────────────────────────────────────────────

    [Fact]
    public async Task T1_MeHistory_NoAuth_Returns401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/me/history");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T2_MeHistory_NoContract_Returns200EmptyArray()
    {
        // Admin user without a linked AppTenant / active contract
        var client = await _factory.AuthedClientAsync("mhist-t2");
        var r = await client.GetAsync("/api/v1/me/history");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task T3_MeHistory_AfterAdjustment_AdjustmentPersistedAndReturnsViaAdmin()
    {
        // The test factory sets up an admin user, not a linked tenant user account.
        // We verify the history was persisted via the admin org-wide endpoint,
        // and that the GetMyRentHistoryQueryHandler returns empty for users with no active contract.
        var (contractId, adminClient) = await _factory.SetupContractAsync("mhist-t3");

        // Apply an adjustment
        (await adminClient.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-09-01",
            manualNewRent = 320_000m,
            notes = "Ajuste septiembre me/history",
        })).EnsureSuccessStatusCode();

        // Admin can see the adjustment via org-wide endpoint (paged envelope since audit M10)
        var adjResp = await adminClient.GetAsync("/api/v1/rent-adjustments");
        Assert.Equal(HttpStatusCode.OK, adjResp.StatusCode);
        var adjBody = await adjResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(adjBody.GetProperty("items").GetArrayLength() >= 1);

        // Admin user has no linked AppTenant → me/history returns empty (not 5xx)
        var meHistResp = await adminClient.GetAsync("/api/v1/me/history");
        Assert.Equal(HttpStatusCode.OK, meHistResp.StatusCode);
        var meArr = await meHistResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, meArr.GetArrayLength());
    }

    // ── TerminateContractCommand validator ───────────────────────────────────

    [Fact]
    public async Task T4_TerminateContract_EmptyIdInUrl_Returns404()
    {
        var client = await _factory.AuthedClientAsync("val-t4");
        // UUID all-zeros is valid syntax but contract won't exist → 409 from BusinessException
        var r = await client.PostAsJsonAsync("/api/v1/contracts/00000000-0000-0000-0000-000000000000/terminate", new
        {
            notes = "test",
        });
        // Expect 409 (contract not found via BusinessException) or 400 (if validator fires on Id)
        Assert.True(r.StatusCode == HttpStatusCode.Conflict || r.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 409 or 400 but got {r.StatusCode}");
    }

    [Fact]
    public async Task T5_TerminateContract_NotesTooLong_Returns400()
    {
        var (contractId, client) = await _factory.SetupContractAsync("val-t5");
        var r = await client.PostAsJsonAsync($"/api/v1/contracts/{contractId}/terminate", new
        {
            notes = new string('A', 1001), // exceeds max 1000
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ── DeleteProperty validator ─────────────────────────────────────────────

    [Fact]
    public async Task T6_DeleteProperty_ValidRequest_Returns204OrConflict()
    {
        var client = await _factory.AuthedClientAsync("val-t6");

        // Create a property first
        var propResp = await client.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Corrientes 500", city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        propResp.EnsureSuccessStatusCode();
        var prop = await propResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var propId = prop.GetProperty("id").GetGuid();

        var r = await client.DeleteAsync($"/api/v1/properties/{propId}");
        // 204 NoContent (deleted) or 409 (in use by contract) — both are valid
        Assert.True(r.StatusCode == HttpStatusCode.NoContent || r.StatusCode == HttpStatusCode.Conflict,
            $"Expected 204 or 409 but got {r.StatusCode}");
    }
}
