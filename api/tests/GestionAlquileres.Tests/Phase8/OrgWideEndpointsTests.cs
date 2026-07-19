using System.Net;
using System.Net.Http.Json;
using System.Text;
using GestionAlquileres.Tests.Phase7;
using Xunit;

namespace GestionAlquileres.Tests.Phase8;

/// <summary>
/// Integration tests for org-wide admin endpoints:
///   GET /api/v1/transactions
///   GET /api/v1/rent-adjustments
///   GET /api/v1/documents
/// </summary>
[Trait("Phase", "Phase8")]
public class OrgWideEndpointsTests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public OrgWideEndpointsTests(Phase7ApiFactory factory) => _factory = factory;

    // ── /api/v1/transactions ─────────────────────────────────────────────────

    [Fact]
    public async Task T1_Transactions_NoAuth_Returns401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/transactions");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T2_Transactions_EmptyOrg_Returns200EmptyArray()
    {
        var c = await _factory.AuthedClientAsync("txall-t2");
        var r = await c.GetAsync("/api/v1/transactions");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, arr.GetArrayLength());
    }

    [Fact]
    public async Task T3_Transactions_AfterPayment_Returns200WithData()
    {
        var (contractId, client) = await _factory.SetupContractAsync("txall-t3");

        // Register a payment
        var payResp = await client.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 250_000m,
            period = "2026-05-01",
            notes = "Pago mayo",
        });
        payResp.EnsureSuccessStatusCode();

        var r = await client.GetAsync("/api/v1/transactions");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.True(arr.GetArrayLength() >= 1);

        // Verify the payment appears
        var found = false;
        foreach (var item in arr.EnumerateArray())
        {
            if (item.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "Payment")
                found = true;
        }
        Assert.True(found, "Payment transaction should appear in org-wide list");
    }

    [Fact]
    public async Task T4_Transactions_CrossOrgIsolation()
    {
        var (contractIdA, clientA) = await _factory.SetupContractAsync("txall-t4a");
        var (_, clientB) = await _factory.SetupContractAsync("txall-t4b");

        // Org A registers a payment
        (await clientA.PostAsJsonAsync($"/api/v1/contracts/{contractIdA}/payments", new
        {
            amount = 100_000m,
            period = "2026-05-01",
            notes = "pago org A",
        })).EnsureSuccessStatusCode();

        // Org B sees no transactions
        var r = await clientB.GetAsync("/api/v1/transactions");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var arr = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, arr.GetArrayLength());
    }

    // ── /api/v1/rent-adjustments ─────────────────────────────────────────────

    [Fact]
    public async Task T5_RentAdjustments_NoAuth_Returns401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/rent-adjustments");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T6_RentAdjustments_EmptyOrg_Returns200EmptyArray()
    {
        var c = await _factory.AuthedClientAsync("adjall-t6");
        var r = await c.GetAsync("/api/v1/rent-adjustments");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        Assert.Equal(0, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task T7_RentAdjustments_AfterAdjustment_Returns200WithData()
    {
        var (contractId, client) = await _factory.SetupContractAsync("adjall-t7");

        // Apply manual adjustment
        var adjResp = await client.PostAsJsonAsync($"/api/v1/contracts/{contractId}/adjust", new
        {
            effectiveDate = "2026-06-01",
            manualNewRent = 300_000m,
            notes = "Ajuste acuerdo partes junio",
        });
        adjResp.EnsureSuccessStatusCode();

        var r = await client.GetAsync("/api/v1/rent-adjustments");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var items = body.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(1, body.GetProperty("total").GetInt32());

        var first = items[0];
        Assert.Equal(250_000m, first.GetProperty("previousRent").GetDecimal());
        Assert.Equal(300_000m, first.GetProperty("newRent").GetDecimal());
        Assert.Equal("Manual", first.GetProperty("adjustmentType").GetString());
    }

    [Fact]
    public async Task T8_RentAdjustments_CrossOrgIsolation()
    {
        var (contractIdA, clientA) = await _factory.SetupContractAsync("adjall-t8a");
        var (_, clientB) = await _factory.SetupContractAsync("adjall-t8b");

        // Org A applies an adjustment
        (await clientA.PostAsJsonAsync($"/api/v1/contracts/{contractIdA}/adjust", new
        {
            effectiveDate = "2026-06-01",
            manualNewRent = 280_000m,
            notes = "ajuste org A",
        })).EnsureSuccessStatusCode();

        // Org B sees no adjustments
        var r = await clientB.GetAsync("/api/v1/rent-adjustments");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    // ── /api/v1/documents ────────────────────────────────────────────────────

    [Fact]
    public async Task T9_Documents_NoAuth_Returns401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T10_Documents_EmptyOrg_Returns200EmptyArray()
    {
        var c = await _factory.AuthedClientAsync("docall-t10");
        var r = await c.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
        Assert.Equal(0, body.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task T11_Documents_AfterUpload_Returns200WithData()
    {
        var (contractId, client) = await _factory.SetupContractAsync("docall-t11");

        using var content = new MultipartFormDataContent();
        content.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes("contrato firmado")) { Headers = { { "Content-Type", "application/pdf" } } },
            "file", "contrato.pdf");
        (await client.PostAsync($"/api/v1/contracts/{contractId}/documents", content)).EnsureSuccessStatusCode();

        var r = await client.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var items = body.GetProperty("items");
        Assert.Equal(1, items.GetArrayLength());
        Assert.Equal(1, body.GetProperty("total").GetInt32());
        Assert.Equal("contrato.pdf", items[0].GetProperty("fileName").GetString());
    }

    [Fact]
    public async Task T12_Documents_CrossOrgIsolation()
    {
        var (contractIdA, clientA) = await _factory.SetupContractAsync("docall-t12a");
        var (_, clientB) = await _factory.SetupContractAsync("docall-t12b");

        // Org A uploads a document
        using var content = new MultipartFormDataContent();
        content.Add(
            new ByteArrayContent(Encoding.UTF8.GetBytes("secret doc")) { Headers = { { "Content-Type", "text/plain" } } },
            "file", "secret.txt");
        (await clientA.PostAsync($"/api/v1/contracts/{contractIdA}/documents", content)).EnsureSuccessStatusCode();

        // Org B sees no documents
        var r = await clientB.GetAsync("/api/v1/documents");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var body = await r.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(0, body.GetProperty("items").GetArrayLength());
    }

    // ── GET /api/v1/transactions/export ──────────────────────────────────────

    [Fact]
    public async Task T13_Export_NoAuth_Returns401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/transactions/export");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T14_Export_WithData_ReturnsCsvFile()
    {
        var (contractId, client) = await _factory.SetupContractAsync("export-t14");

        // Register a payment so there's data to export
        (await client.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 250_000m,
            period = "2026-07-01",
            notes = "Pago julio",
        })).EnsureSuccessStatusCode();

        var r = await client.GetAsync("/api/v1/transactions/export");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Contains("text/csv", r.Content.Headers.ContentType?.MediaType ?? "");

        var csv = await r.Content.ReadAsStringAsync();
        Assert.Contains("Id,ContractId,Tipo", csv);       // header row
        Assert.Contains("Payment", csv);                  // the payment row
        Assert.Contains("250000", csv);                   // the amount
    }

    [Fact]
    public async Task T15_Export_EmptyOrg_ReturnsCsvHeaderOnly()
    {
        var client = await _factory.AuthedClientAsync("export-t15");
        var r = await client.GetAsync("/api/v1/transactions/export");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);

        var csv = await r.Content.ReadAsStringAsync();
        var lines = csv.Trim().Split('\n');
        Assert.Single(lines); // only the header, no data rows
    }
}
