using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Application.Features.RentHistory.DTOs;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Tests.Phase5;

[Trait("Phase", "Phase5")]
public class ContractRentControllerTests : IClassFixture<Phase5ApiFactory>
{
    private readonly Phase5ApiFactory _factory;

    public ContractRentControllerTests(Phase5ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    // ── Auth ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task T1_Get_history_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync($"/api/v1/contracts/{Guid.NewGuid()}/history");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T2_Get_transactions_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync($"/api/v1/contracts/{Guid.NewGuid()}/transactions");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    // ── Manual adjustment ────────────────────────────────────────────────────

    [Fact]
    public async Task T3_Apply_manual_adjustment_returns_200_and_updates_rent()
    {
        var c = await _factory.AuthedClientAsync("rent-t3");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01",
            manualNewRent = 250_000m,
            notes = "Acuerdo entre partes",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<RentHistoryDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(200_000m, dto!.PreviousRent);
        Assert.Equal(250_000m, dto.NewRent);
        Assert.Equal(AdjustmentType.Manual, dto.AdjustmentType);
        Assert.Equal("Acuerdo entre partes", dto.Notes);

        // Verify contract MonthlyRent was updated
        var contractR = await c.GetAsync($"/api/v1/contracts/{contractId}");
        var contract = await contractR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);
        Assert.Equal(250_000m, contract!.MonthlyRent);
    }

    [Fact]
    public async Task T4_Apply_manual_adjustment_without_notes_returns_409()
    {
        var c = await _factory.AuthedClientAsync("rent-t4");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01",
            manualNewRent = 250_000m,
            notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task T5_Apply_manual_adjustment_without_manual_new_rent_returns_409()
    {
        var c = await _factory.AuthedClientAsync("rent-t5");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01",
            manualNewRent = (decimal?)null,
            notes = "Alguna nota",
        });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task T6_Apply_ICL_adjustment_without_index_data_returns_409()
    {
        // Create an ICL contract to test index-not-found path
        var c = await _factory.AuthedClientAsync("rent-t6");
        var propResp = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Rivadavia 500", city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        propResp.EnsureSuccessStatusCode();
        var prop = await propResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var propId = prop.GetProperty("id").GetGuid();

        var tenantResp = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Pedro", lastName = "López", dni = "99887766",
            email = (string?)null, phone = (string?)null,
        });
        tenantResp.EnsureSuccessStatusCode();
        var ten = await tenantResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var tenantId = ten.GetProperty("id").GetGuid();

        var contractResp = await c.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = propId,
            appTenantId = tenantId,
            startDate = "2026-01-01",
            endDate = "2028-01-01",
            monthlyRent = 150_000m,
            currency = "ARS",
            adjustmentType = "ICL",
            adjustmentFrequency = "Quarterly",
            dayOfMonth = 1,
            depositAmount = (decimal?)null,
            notes = (string?)null,
        });
        contractResp.EnsureSuccessStatusCode();
        var contract = await contractResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var contractId = contract.GetProperty("id").GetGuid();

        // No index data seeded → should fail with 409
        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01",
            manualNewRent = (decimal?)null,
            notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    // ── Payments ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task T7_Register_payment_returns_200_with_payment_transaction()
    {
        var c = await _factory.AuthedClientAsync("rent-t7");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 200_000m,
            period = "2026-02-01",
            notes = "Pago enero",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(contractId, dto!.ContractId);
        Assert.Equal(TransactionType.Payment, dto.Type);
        Assert.Equal(200_000m, dto.Amount);
    }

    [Fact]
    public async Task T8_Register_payment_with_zero_amount_returns_400()
    {
        var c = await _factory.AuthedClientAsync("rent-t8");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 0m,
            period = "2026-02-01",
            notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    // ── Manual charges ────────────────────────────────────────────────────────

    [Fact]
    public async Task T9_Register_manual_charge_without_notes_returns_400()
    {
        var c = await _factory.AuthedClientAsync("rent-t9");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualDebit",
            amount = 10_000m,
            period = "2026-02-01",
            notes = "",
        });

        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task T10_Register_manual_debit_returns_200_with_correct_type()
    {
        var c = await _factory.AuthedClientAsync("rent-t10");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        var r = await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualDebit",
            amount = 15_000m,
            period = "2026-02-01",
            notes = "Multa por daño en propiedad",
        });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<TransactionDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(TransactionType.ManualDebit, dto!.Type);
        Assert.Equal(15_000m, dto.Amount);
    }

    // ── History & Transactions queries ────────────────────────────────────────

    [Fact]
    public async Task T11_GetHistory_returns_adjustment_records()
    {
        var c = await _factory.AuthedClientAsync("rent-t11");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        // Apply adjustment first
        await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-04-01",
            manualNewRent = 220_000m,
            notes = "Primer ajuste",
        });

        var r = await c.GetAsync($"/api/v1/contracts/{contractId}/history");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var list = await r.Content.ReadFromJsonAsync<List<RentHistoryDto>>(JsonOpts);
        Assert.NotNull(list);
        Assert.Single(list!);
        Assert.Equal(220_000m, list![0].NewRent);
        Assert.Equal("Primer ajuste", list[0].Notes);
    }

    [Fact]
    public async Task T12_GetTransactions_returns_payment_and_charge_records()
    {
        var c = await _factory.AuthedClientAsync("rent-t12");
        var (contractId, _, _) = await _factory.SetupContractAsync(c);

        await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 200_000m,
            period = "2026-02-01",
            notes = (string?)null,
        });

        await c.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualCredit",
            amount = 5_000m,
            period = "2026-02-01",
            notes = "Descuento acordado",
        });

        var r = await c.GetAsync($"/api/v1/contracts/{contractId}/transactions");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var list = await r.Content.ReadFromJsonAsync<List<TransactionDto>>(JsonOpts);
        Assert.NotNull(list);
        Assert.Equal(2, list!.Count);
        Assert.Contains(list, t => t.Type == TransactionType.Payment);
        Assert.Contains(list, t => t.Type == TransactionType.ManualCredit);
    }

    // ── Cross-org isolation ───────────────────────────────────────────────────

    [Fact]
    public async Task T13_Cross_org_history_and_transactions_return_empty()
    {
        var c1 = await _factory.AuthedClientAsync("rent-t13a");
        var c2 = await _factory.AuthedClientAsync("rent-t13b");

        var (contractId, _, _) = await _factory.SetupContractAsync(c1);

        await c1.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 200_000m,
            period = "2026-02-01",
            notes = (string?)null,
        });

        // Org2 queries with Org1's contractId — global filter returns empty
        var histR = await c2.GetAsync($"/api/v1/contracts/{contractId}/history");
        Assert.Equal(HttpStatusCode.OK, histR.StatusCode);
        var hist = await histR.Content.ReadFromJsonAsync<List<RentHistoryDto>>(JsonOpts);
        Assert.Empty(hist!);

        var txR = await c2.GetAsync($"/api/v1/contracts/{contractId}/transactions");
        Assert.Equal(HttpStatusCode.OK, txR.StatusCode);
        var txs = await txR.Content.ReadFromJsonAsync<List<TransactionDto>>(JsonOpts);
        Assert.Empty(txs!);
    }
}
