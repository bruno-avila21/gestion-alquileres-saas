using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Application.Features.Auth.Commands;
using GestionAlquileres.Application.Features.Indexes.DTOs;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GestionAlquileres.Tests.Phase2.API;

[Trait("Phase", "Phase2")]
public class IndexesControllerTests : IClassFixture<IndexesControllerTests.ApiFactory>
{
    public sealed class StubBcra : IBcraApiClient
    {
        public Func<DateOnly, DateOnly, IReadOnlyList<BcraIndexPoint>>? Responder;
        public Exception? ToThrow;
        public int Calls;

        public Task<IReadOnlyList<BcraIndexPoint>> GetIclAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
        {
            Calls++;
            if (ToThrow is not null) throw ToThrow;
            return Task.FromResult(Responder?.Invoke(desde, hasta) ?? (IReadOnlyList<BcraIndexPoint>)Array.Empty<BcraIndexPoint>());
        }
    }

    public sealed class StubIndec : IIndecApiClient
    {
        public Func<DateOnly, DateOnly, IReadOnlyList<IndecIndexPoint>>? Responder;
        public Exception? ToThrow;
        public int Calls;

        public Task<IReadOnlyList<IndecIndexPoint>> GetIpcAsync(DateOnly desde, DateOnly hasta, CancellationToken ct = default)
        {
            Calls++;
            if (ToThrow is not null) throw ToThrow;
            return Task.FromResult(Responder?.Invoke(desde, hasta) ?? (IReadOnlyList<IndecIndexPoint>)Array.Empty<IndecIndexPoint>());
        }
    }

    public class ApiFactory : WebApplicationFactory<Program>
    {
        public string DbName { get; } = "IndexesControllerTestsDb_" + Guid.NewGuid();
        public StubBcra StubBcra { get; } = new();
        public StubIndec StubIndec { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((ctx, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience",
                    ["JwtSettings:SecretKey"] = "THIS_IS_A_TEST_SECRET_KEY_32_CHARS_MINIMUM",
                    ["JwtSettings:ExpiryHours"] = "1",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x",
                    ["ConnectionStrings:HangfireConnection"] = "Host=localhost;Database=x"
                });
            });
            builder.ConfigureServices(services =>
            {
                var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDesc is not null) services.Remove(dbDesc);
                services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(DbName));

                // Remove Hangfire hosted services
                var hfDescriptors = services
                    .Where(d => d.ServiceType.FullName?.StartsWith("Microsoft.Extensions.Hosting.IHostedService") == true
                             && d.ImplementationType?.FullName?.Contains("Hangfire") == true)
                    .ToList();
                foreach (var d in hfDescriptors) services.Remove(d);

                var hfByName = services
                    .Where(d => d.ImplementationType?.Assembly.FullName?.Contains("Hangfire") == true
                             || d.ImplementationFactory?.Method.DeclaringType?.Assembly.FullName?.Contains("Hangfire") == true)
                    .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                    .ToList();
                foreach (var d in hfByName) services.Remove(d);

                // Replace external API clients with stubs
                var toRemove = services.Where(d =>
                    d.ServiceType == typeof(IBcraApiClient) ||
                    d.ServiceType == typeof(IIndecApiClient)).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddSingleton(StubBcra);
                services.AddSingleton(StubIndec);
                services.AddSingleton<IBcraApiClient>(_ => StubBcra);
                services.AddSingleton<IIndecApiClient>(_ => StubIndec);

                // Guarantee token generation and validation use the same key.
                // Program.cs reads JwtSettings directly from builder.Configuration at startup
                // (before WebApplicationFactory.ConfigureAppConfiguration sources are added),
                // while JwtService reads lazily via IOptions. PostConfigure ensures both sides
                // always use these known test values.
                const string testKey = "THIS_IS_A_TEST_SECRET_KEY_32_CHARS_MINIMUM";
                services.PostConfigure<JwtSettings>(o =>
                {
                    o.Issuer = "TestIssuer";
                    o.Audience = "TestAudience";
                    o.SecretKey = testKey;
                    o.ExpiryHours = 1;
                });
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
                {
                    o.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "TestIssuer",
                        ValidAudience = "TestAudience",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(testKey)),
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });
            });
        }
    }

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts = new(System.Text.Json.JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory;

    public IndexesControllerTests(ApiFactory factory)
    {
        _factory = factory;
        // Reset stub state before each test (tests run sequentially within class)
        _factory.StubBcra.Responder = null;
        _factory.StubBcra.ToThrow = null;
        _factory.StubBcra.Calls = 0;
        _factory.StubIndec.Responder = null;
        _factory.StubIndec.ToThrow = null;
        _factory.StubIndec.Calls = 0;
    }

    private async Task<HttpClient> AuthedClientAsync(string slug)
    {
        var c = _factory.CreateClient();

        var regResp = await c.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand($"{slug} Org", slug, $"a@{slug}.com", "Password123", "A", "A"));
        var regBody = await regResp.Content.ReadAsStringAsync();
        Assert.True(regResp.IsSuccessStatusCode,
            $"register-org failed {regResp.StatusCode}: {regBody}");

        var loginResp = await c.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand($"a@{slug}.com", "Password123", slug));
        var loginBody = await loginResp.Content.ReadAsStringAsync();
        Assert.True(loginResp.IsSuccessStatusCode,
            $"login failed {loginResp.StatusCode}: {loginBody}");

        var body = await loginResp.Content.ReadFromJsonAsync<AuthResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.Token), $"Token is null/empty. Login body: {loginBody}");

        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.Token);
        return c;
    }

    private async Task SeedIndexValue(DateOnly period, IndexType type, decimal value)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Set<IndexValue>().Add(new IndexValue
        {
            IndexType = type,
            Period = period,
            Value = value,
            Source = type == IndexType.ICL ? "BCRA" : "INDEC",
            FetchedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task T1_GetIndexes_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/indexes?type=ICL&from=2024-01-01&to=2024-03-31");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T2_Sync_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/indexes/sync", new { indexType = "ICL", period = "2024-03-01" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T3_Sync_happy_path_persists_and_returns_200()
    {
        // Unique period: 2024-01 (past, not used by any other test)
        var period = new DateOnly(2024, 1, 1);
        _factory.StubBcra.Responder = (_, _) => new[] { new BcraIndexPoint(new DateOnly(2024, 1, 15), 101.5m) };

        var c = await AuthedClientAsync("t3org");
        var r = await c.PostAsJsonAsync("/api/v1/indexes/sync", new { indexType = "ICL", period = "2024-01-01" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var result = await r.Content.ReadFromJsonAsync<SyncIndexResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.False(result!.WasFallback);
        Assert.False(result.AlreadyExisted);
        Assert.Equal(101.5m, result.IndexValue.Value);
        Assert.Equal(period, result.IndexValue.Period);
        Assert.Equal("BCRA", result.IndexValue.Source);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var row = await db.Set<IndexValue>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Period == period && v.IndexType == IndexType.ICL);
        Assert.NotNull(row);
        Assert.Equal(101.5m, row!.Value);
    }

    [Fact]
    public async Task T4_Sync_idempotent_second_call_returns_AlreadyExisted_without_API()
    {
        // Unique period: 2024-02
        _factory.StubBcra.Responder = (_, _) => new[] { new BcraIndexPoint(new DateOnly(2024, 2, 1), 102.0m) };

        var c = await AuthedClientAsync("t4org");
        var body = new { indexType = "ICL", period = "2024-02-01" };

        await c.PostAsJsonAsync("/api/v1/indexes/sync", body);
        var callsAfterFirst = _factory.StubBcra.Calls;
        var r2 = await c.PostAsJsonAsync("/api/v1/indexes/sync", body);

        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var result = await r2.Content.ReadFromJsonAsync<SyncIndexResult>(JsonOpts);
        Assert.True(result!.AlreadyExisted);
        Assert.Equal(callsAfterFirst, _factory.StubBcra.Calls); // no extra API call
    }

    [Fact]
    public async Task T5_Sync_future_period_returns_400()
    {
        var c = await AuthedClientAsync("t5org");
        var r = await c.PostAsJsonAsync("/api/v1/indexes/sync", new { indexType = "ICL", period = "2099-01-01" });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task T6_Sync_API_fails_and_no_fallback_returns_409()
    {
        // Use IPC — no IPC data exists at this point in the test sequence, so no fallback is available.
        // ICL would find T3/T4 data (shared DB), causing a 200-Fallback instead of 409.
        _factory.StubIndec.ToThrow = new HttpRequestException("down");

        var c = await AuthedClientAsync("t6org");
        var r = await c.PostAsJsonAsync("/api/v1/indexes/sync", new { indexType = "IPC", period = "2024-03-01" });
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task T7_Sync_API_fails_but_previous_value_exists_returns_200_Fallback()
    {
        // Seed ICL 2024-04 (more recent than T3/T4 ICL data) so it becomes the fallback.
        await SeedIndexValue(new DateOnly(2024, 4, 1), IndexType.ICL, 99.0m);
        _factory.StubBcra.ToThrow = new HttpRequestException("down");

        var c = await AuthedClientAsync("t7org");
        var r = await c.PostAsJsonAsync("/api/v1/indexes/sync", new { indexType = "ICL", period = "2024-05-01" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var result = await r.Content.ReadFromJsonAsync<SyncIndexResult>(JsonOpts);
        Assert.True(result!.WasFallback);
        Assert.Equal(new DateOnly(2024, 4, 1), result.IndexValue.Period);
    }

    [Fact]
    public async Task T8_GetIndexes_returns_200_with_rows_in_range()
    {
        // Use year 2023 (past, unique) to avoid conflicts with other tests and validator (To <= today).
        await SeedIndexValue(new DateOnly(2023, 1, 1), IndexType.ICL, 98.0m);
        await SeedIndexValue(new DateOnly(2023, 2, 1), IndexType.ICL, 99.0m);
        await SeedIndexValue(new DateOnly(2023, 3, 1), IndexType.ICL, 100.0m);
        await SeedIndexValue(new DateOnly(2023, 4, 1), IndexType.ICL, 101.0m);
        await SeedIndexValue(new DateOnly(2023, 2, 1), IndexType.IPC, 50.0m);
        await SeedIndexValue(new DateOnly(2023, 3, 1), IndexType.IPC, 51.0m);

        var c = await AuthedClientAsync("t8org");
        var r = await c.GetAsync("/api/v1/indexes?type=ICL&from=2023-02-01&to=2023-03-31");

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var rows = await r.Content.ReadFromJsonAsync<IReadOnlyList<IndexValueDto>>(JsonOpts);
        Assert.NotNull(rows);
        Assert.Equal(2, rows!.Count);
        Assert.All(rows, v => Assert.Equal(IndexType.ICL, v.IndexType));
    }

    [Fact]
    public async Task T9_GetIndexes_invalid_range_returns_400()
    {
        var c = await AuthedClientAsync("t9org");
        var r = await c.GetAsync("/api/v1/indexes?type=ICL&from=2024-03-01&to=2024-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task T10_Sync_IPC_happy_path()
    {
        // Unique period: 2024-06 (past IPC, no conflict with T6 IPC 2024-03 or T8 IPC 2023-xx)
        _factory.StubIndec.Responder = (_, _) => new[] { new IndecIndexPoint(new DateOnly(2024, 6, 1), 4500.0m) };

        var c = await AuthedClientAsync("t10org");
        var r = await c.PostAsJsonAsync("/api/v1/indexes/sync", new { indexType = "IPC", period = "2024-06-01" });

        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var result = await r.Content.ReadFromJsonAsync<SyncIndexResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.Equal("INDEC", result!.IndexValue.Source);
        Assert.Equal(4500.0m, result.IndexValue.Value);
    }
}
