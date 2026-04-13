using System.Net;
using GestionAlquileres.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestionAlquileres.Tests.Phase2.Infrastructure;

public class IndecApiClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public string? LastRequestUri { get; private set; }
        public HttpStatusCode ResponseStatus { get; set; } = HttpStatusCode.OK;
        public string ResponseBody { get; set; } = "";
        public int CallCount { get; private set; }
        public Exception? ThrowOnSend { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            LastRequestUri = request.RequestUri?.ToString();
            if (ThrowOnSend is not null) throw ThrowOnSend;
            return Task.FromResult(new HttpResponseMessage(ResponseStatus)
            {
                Content = new StringContent(ResponseBody, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }

    private static IndecApiClient CreateClient(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://apis.datos.gob.ar") };
        return new IndecApiClient(http, NullLogger<IndecApiClient>.Instance);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIpcAsync_constructs_correct_series_url()
    {
        var handler = new StubHandler
        {
            ResponseBody = """{"data":[],"count":0,"meta":[]}"""
        };
        var client = CreateClient(handler);
        var desde = new DateOnly(2024, 1, 1);
        var hasta = new DateOnly(2024, 3, 31);

        await client.GetIpcAsync(desde, hasta);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("series/api/series/?ids=148.3_INIVELNAL_DICI_M_26", handler.LastRequestUri);
        Assert.Contains("start_date=2024-01-01", handler.LastRequestUri);
        Assert.Contains("end_date=2024-03-31", handler.LastRequestUri);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIpcAsync_parses_array_of_arrays_format()
    {
        var handler = new StubHandler
        {
            ResponseBody = """{"data":[["2024-01-01", 4500.5],["2024-02-01", 4650.2]],"count":2,"meta":[]}"""
        };
        var client = CreateClient(handler);

        var result = await client.GetIpcAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 31));

        Assert.Equal(2, result.Count);
        Assert.Equal(new DateOnly(2024, 1, 1), result[0].Fecha);
        Assert.Equal(4500.5m, result[0].Valor);
        Assert.Equal(new DateOnly(2024, 2, 1), result[1].Fecha);
        Assert.Equal(4650.2m, result[1].Valor);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIpcAsync_returns_empty_when_data_null_or_missing()
    {
        var handler = new StubHandler
        {
            ResponseBody = """{"data":null,"count":0,"meta":[]}"""
        };
        var client = CreateClient(handler);

        var result = await client.GetIpcAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 31));

        Assert.Empty(result);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIpcAsync_throws_HttpRequestException_on_5xx()
    {
        var handler = new StubHandler
        {
            ResponseStatus = HttpStatusCode.InternalServerError,
            ResponseBody = """{"error":"internal server error"}"""
        };
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetIpcAsync(new DateOnly(2024, 1, 1), new DateOnly(2024, 3, 31)));
    }
}
