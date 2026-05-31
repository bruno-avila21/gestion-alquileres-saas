using System.Net;
using System.Text;
using GestionAlquileres.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestionAlquileres.Tests.Phase4;

public class IndicesApiClientTests
{
    private sealed class FakeHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest;
        public HttpStatusCode Status = HttpStatusCode.OK;
        public string Json = """{"status":"NewlySynced","value":{"value":123.45,"period":"2026-03-01"}}""";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(Status)
            {
                Content = new StringContent(Json, Encoding.UTF8, "application/json"),
            });
        }
    }

    private static IndicesApiClient Client(FakeHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("http://indices.test") },
            NullLogger<IndicesApiClient>.Instance);

    [Fact]
    public async Task GetIcl_calls_sync_endpoint_and_parses_value()
    {
        var handler = new FakeHandler();
        var points = await Client(handler).GetIclAsync(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), default);

        Assert.Single(points);
        Assert.Equal(123.45m, points[0].Valor);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/v1/indices/ICL/sync", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GetIpc_calls_ipc_sync_endpoint()
    {
        var handler = new FakeHandler
        {
            Json = """{"status":"NewlySynced","value":{"value":7777.7,"period":"2026-03-01"}}""",
        };
        var points = await Client(handler).GetIpcAsync(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), default);

        Assert.Single(points);
        Assert.Equal(7777.7m, points[0].Valor);
        Assert.Equal("/v1/indices/IPC/sync", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task Propagates_http_error_so_saas_can_fall_back()
    {
        var handler = new FakeHandler { Status = HttpStatusCode.InternalServerError };
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            Client(handler).GetIclAsync(new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 31), default));
    }
}
