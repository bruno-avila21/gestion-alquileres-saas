using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Application.Features.Auth.Commands;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace GestionAlquileres.Tests.Phase5;

public class Phase5ApiFactory : WebApplicationFactory<Program>
{
    public string DbName { get; } = "Phase5TestsDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:SecretKey"] = "THIS_IS_A_TEST_SECRET_KEY_32_CHARS_MINIMUM",
                ["JwtSettings:ExpiryHours"] = "1",
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x",
                ["ConnectionStrings:HangfireConnection"] = "Host=localhost;Database=x",
            });
        });
        builder.ConfigureServices(services =>
        {
            var dbDesc = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbDesc is not null) services.Remove(dbDesc);
            services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(DbName));

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
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });
        });
    }

    public async Task<HttpClient> AuthedClientAsync(string slug)
    {
        var c = CreateClient();
        var regResp = await c.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand($"{slug} Org", slug, $"admin@{slug}.com", "Password123!", "Admin", "Test"));
        regResp.EnsureSuccessStatusCode();

        var loginResp = await c.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand($"admin@{slug}.com", "Password123!", slug));
        loginResp.EnsureSuccessStatusCode();

        var body = await loginResp.Content.ReadFromJsonAsync<AuthResponseDto>(
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return c;
    }

    public async Task<(Guid contractId, Guid propertyId, Guid tenantId)> SetupContractAsync(HttpClient client)
    {
        var propResp = await client.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Corrientes 800", city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        propResp.EnsureSuccessStatusCode();
        var prop = await propResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var propId = prop.GetProperty("id").GetGuid();

        var tenantResp = await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Ana", lastName = "García", dni = "45678901",
            email = (string?)null, phone = (string?)null,
        });
        tenantResp.EnsureSuccessStatusCode();
        var ten = await tenantResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var tenantId = ten.GetProperty("id").GetGuid();

        var contractResp = await client.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = propId,
            appTenantId = tenantId,
            startDate = "2026-01-01",
            endDate = "2028-01-01",
            monthlyRent = 200_000m,
            currency = "ARS",
            adjustmentType = "Manual",
            adjustmentFrequency = "Quarterly",
            dayOfMonth = 1,
            depositAmount = (decimal?)null,
            notes = (string?)null,
        });
        contractResp.EnsureSuccessStatusCode();
        var contract = await contractResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var contractId = contract.GetProperty("id").GetGuid();

        return (contractId, propId, tenantId);
    }
}
