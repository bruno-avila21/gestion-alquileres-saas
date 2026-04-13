using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Auth.Commands;
using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAlquileres.Tests;

public class AuthTests : IClassFixture<AuthTests.ApiFactory>
{
    public class ApiFactory : WebApplicationFactory<Program>
    {
        public string DbName { get; } = "AuthTestsDb_" + Guid.NewGuid();

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
                // Replace AppDbContext with InMemory for tests
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor is not null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(o =>
                    o.UseInMemoryDatabase(DbName));

                // Remove all Hangfire hosted services so they don't hang during WebApplicationFactory disposal
                var hfDescriptors = services
                    .Where(d => d.ServiceType.FullName?.StartsWith("Microsoft.Extensions.Hosting.IHostedService") == true
                             && d.ImplementationType?.FullName?.Contains("Hangfire") == true)
                    .ToList();
                foreach (var d in hfDescriptors) services.Remove(d);

                // Also remove by implementation type name in case of factory registrations
                var hfByName = services
                    .Where(d => d.ImplementationType?.Assembly.FullName?.Contains("Hangfire") == true
                             || d.ImplementationFactory?.Method.DeclaringType?.Assembly.FullName?.Contains("Hangfire") == true)
                    .Where(d => d.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                    .ToList();
                foreach (var d in hfByName) services.Remove(d);
            });
        }
    }

    private readonly ApiFactory _factory;
    public AuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task RegisterOrg_creates_org_and_admin_returns_jwt_with_org_id_claim()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterOrgCommand(
            OrganizationName: "Acme Inmobiliaria",
            Slug: "acme",
            AdminEmail: "admin@acme.com",
            AdminPassword: "SuperSecret123",
            AdminFirstName: "Ada",
            AdminLastName: "Lovelace");

        var response = await client.PostAsJsonAsync("/api/v1/auth/register-org", cmd);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Equal("Admin", body.Role);
        Assert.Equal("acme", body.OrganizationSlug);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(body.Token);
        Assert.Contains(jwt.Claims, c => c.Type == "org_id" && Guid.TryParse(c.Value, out _));
        Assert.Contains(jwt.Claims, c => c.Type.EndsWith("role") && c.Value == "Admin");
        Assert.Contains(jwt.Claims, c => c.Type.EndsWith("email") && c.Value == "admin@acme.com");
    }

    [Fact]
    public async Task RegisterOrg_rejects_duplicate_slug_with_409()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterOrgCommand("Beta", "beta-org", "b@b.com", "Password123", "B", "B");
        var r1 = await client.PostAsJsonAsync("/api/v1/auth/register-org", cmd);
        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        var r2 = await client.PostAsJsonAsync("/api/v1/auth/register-org", cmd with { AdminEmail = "b2@b.com" });
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task RegisterOrg_with_invalid_email_returns_400()
    {
        var client = _factory.CreateClient();
        var cmd = new RegisterOrgCommand("Gamma", "gamma", "not-an-email", "Password123", "G", "G");
        var r = await client.PostAsJsonAsync("/api/v1/auth/register-org", cmd);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Login_with_correct_password_returns_jwt()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("Delta", "delta", "d@d.com", "Password123", "D", "D"));

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("d@d.com", "Password123", "delta"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("Epsilon", "epsilon", "e@e.com", "Password123", "E", "E"));
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginCommand("e@e.com", "WrongPassword", "epsilon"));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task TenantLogin_rejects_Admin_role_with_401()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("Zeta", "zeta", "z@z.com", "Password123", "Z", "Z"));
        // Admin trying to use tenant-login endpoint
        var login = await client.PostAsJsonAsync("/api/v1/auth/tenant-login",
            new TenantLoginCommand("z@z.com", "Password123", "zeta"));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task TenantLogin_accepts_Tenant_role()
    {
        // Seed a Tenant user directly via DI
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/v1/auth/register-org",
            new RegisterOrgCommand("Eta", "eta", "admin@eta.com", "Password123", "A", "A"));

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var org = await db.Organizations.IgnoreQueryFilters().FirstAsync(o => o.Slug == "eta");
            db.Users.Add(new User
            {
                OrganizationId = org.Id,
                Email = "tenant@eta.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                FirstName = "T",
                LastName = "T",
                Role = UserRole.Tenant,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var login = await client.PostAsJsonAsync("/api/v1/auth/tenant-login",
            new TenantLoginCommand("tenant@eta.com", "Password123", "eta"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var body = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.Equal("Tenant", body!.Role);
    }

    [Fact]
    public async Task Health_endpoint_is_reachable()
    {
        var client = _factory.CreateClient();
        var r = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
    }
}
