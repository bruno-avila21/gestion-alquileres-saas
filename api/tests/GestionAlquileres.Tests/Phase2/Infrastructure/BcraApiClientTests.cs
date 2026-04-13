using System.Net;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;

namespace GestionAlquileres.Tests.Phase2.Infrastructure;

public class BcraApiClientTests
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

    private static BcraApiClient CreateClient(StubHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.bcra.gob.ar") };
        return new BcraApiClient(http, NullLogger<BcraApiClient>.Instance);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIclAsync_constructs_correct_v1_url()
    {
        var handler = new StubHandler
        {
            ResponseBody = """{"status":200,"results":[]}"""
        };
        var client = CreateClient(handler);
        var desde = new DateOnly(2024, 3, 1);
        var hasta = new DateOnly(2024, 3, 31);

        await client.GetIclAsync(desde, hasta);

        Assert.NotNull(handler.LastRequestUri);
        Assert.EndsWith("/estadisticas/v1/datosvariable/7988/2024-03-01/2024-03-31", handler.LastRequestUri);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIclAsync_parses_DD_MM_YYYY_dates_to_DateOnly()
    {
        var handler = new StubHandler
        {
            ResponseBody = """{"status":200,"results":[{"fecha":"15/03/2024","valor":1.234}]}"""
        };
        var client = CreateClient(handler);

        var result = await client.GetIclAsync(new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 31));

        Assert.Single(result);
        Assert.Equal(new DateOnly(2024, 3, 15), result[0].Fecha);
        Assert.Equal(1.234m, result[0].Valor);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIclAsync_returns_empty_when_results_null()
    {
        var handler = new StubHandler
        {
            ResponseBody = """{"status":200,"results":null}"""
        };
        var client = CreateClient(handler);

        var result = await client.GetIclAsync(new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 31));

        Assert.Empty(result);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIclAsync_returns_multiple_data_points_in_order()
    {
        var handler = new StubHandler
        {
            ResponseBody = """
            {
                "status": 200,
                "results": [
                    {"fecha":"01/03/2024","valor":1.100},
                    {"fecha":"02/03/2024","valor":1.200},
                    {"fecha":"03/03/2024","valor":1.300},
                    {"fecha":"04/03/2024","valor":1.400},
                    {"fecha":"05/03/2024","valor":1.500}
                ]
            }
            """
        };
        var client = CreateClient(handler);

        var result = await client.GetIclAsync(new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 31));

        Assert.Equal(5, result.Count);
        Assert.Equal(new DateOnly(2024, 3, 1), result[0].Fecha);
        Assert.Equal(new DateOnly(2024, 3, 5), result[4].Fecha);
    }

    [Fact]
    [Trait("Phase", "Phase2")]
    public async Task GetIclAsync_propagates_HttpRequestException_on_5xx()
    {
        var handler = new StubHandler
        {
            ResponseStatus = HttpStatusCode.InternalServerError,
            ResponseBody = """{"error":"internal server error"}"""
        };
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetIclAsync(new DateOnly(2024, 3, 1), new DateOnly(2024, 3, 31)));
    }
}
