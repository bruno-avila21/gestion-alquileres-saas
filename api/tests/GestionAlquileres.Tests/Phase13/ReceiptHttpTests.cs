using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GestionAlquileres.Tests.Phase7;

namespace GestionAlquileres.Tests.Phase13;

/// <summary>
/// Bloque PDF recibos/liquidaciones, parte B — extremo a extremo vía HTTP. Usa Phase7ApiFactory
/// (mismo motivo que Phase11/Phase12: sólo necesita el host completo con auth + tenant, storage
/// Local). La numeración en sí (mismo número / números consecutivos) se prueba a nivel handler en
/// PaymentReceiptPdfHandlerTests: acá sólo interesan los códigos de estado del endpoint HTTP, que
/// nunca llegan a incrementar el contador (404 y 409 salen antes de esa rama).
/// </summary>
[Trait("Phase", "Phase13")]
public class ReceiptHttpTests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public ReceiptHttpTests(Phase7ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Transaccion_inexistente_devuelve_404()
    {
        var (_, client) = await _factory.SetupContractAsync("recibo404");

        var resp = await client.GetAsync($"/api/v1/transactions/{Guid.NewGuid()}/receipt");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Transaccion_que_no_es_pago_devuelve_409()
    {
        var (contractId, client) = await _factory.SetupContractAsync("recibo409");

        var chargeResp = await client.PostAsJsonAsync($"/api/v1/contracts/{contractId}/charges", new
        {
            type = "ManualDebit",
            amount = 5000m,
            period = "2026-03-01",
            notes = "Punitorio de prueba",
        });
        chargeResp.EnsureSuccessStatusCode();
        var charge = await chargeResp.Content.ReadFromJsonAsync<JsonElement>();
        var chargeId = charge.GetProperty("id").GetGuid();

        var resp = await client.GetAsync($"/api/v1/transactions/{chargeId}/receipt");

        Assert.Equal(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [Fact]
    public async Task Transaccion_de_pago_devuelve_el_pdf()
    {
        var (contractId, client) = await _factory.SetupContractAsync("recibo200");

        var payResp = await client.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 120000m,
            period = "2026-03-01",
            notes = (string?)null,
        });
        payResp.EnsureSuccessStatusCode();
        var payment = await payResp.Content.ReadFromJsonAsync<JsonElement>();
        var paymentId = payment.GetProperty("id").GetGuid();

        var resp = await client.GetAsync($"/api/v1/transactions/{paymentId}/receipt");

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        Assert.Equal("application/pdf", resp.Content.Headers.ContentType?.MediaType);
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 1024);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public async Task Admin_de_otra_organizacion_no_obtiene_el_recibo_ajeno()
    {
        var (contractIdA, clientA) = await _factory.SetupContractAsync("recibotenanta");
        var (_, clientB) = await _factory.SetupContractAsync("recibotenantb");

        var payResp = await clientA.PostAsJsonAsync($"/api/v1/contracts/{contractIdA}/payments", new
        {
            amount = 80000m,
            period = "2026-03-01",
            notes = (string?)null,
        });
        payResp.EnsureSuccessStatusCode();
        var payment = await payResp.Content.ReadFromJsonAsync<JsonElement>();
        var paymentId = payment.GetProperty("id").GetGuid();

        // El admin de la organización B pide el recibo de una transacción de la organización A.
        var resp = await clientB.GetAsync($"/api/v1/transactions/{paymentId}/receipt");

        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
