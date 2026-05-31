using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Application.Features.Dashboard;
using GestionAlquileres.Application.Features.Transactions.DTOs;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Tests.Phase6;

[Trait("Phase", "Phase6")]
public class DashboardAndMeTests : IClassFixture<Phase6ApiFactory>
{
    private readonly Phase6ApiFactory _factory;

    public DashboardAndMeTests(Phase6ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    // ── Dashboard ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task T1_Dashboard_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/dashboard");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T2_Dashboard_empty_org_returns_zeros()
    {
        var c = await _factory.AuthedClientAsync("dash-t2");
        var r = await c.GetAsync("/api/v1/dashboard");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<DashboardDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(0, dto!.ActiveContractsCount);
        Assert.Equal(0m, dto.MonthlyRevenue);
        Assert.Empty(dto.RecentTransactions);
    }

    [Fact]
    public async Task T3_Dashboard_with_contracts_returns_correct_stats()
    {
        var (_, _, c) = await _factory.SetupContractWithTenantAsync("dash-t3");

        var r = await c.GetAsync("/api/v1/dashboard");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<DashboardDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(1, dto!.ActiveContractsCount);
        Assert.Equal(300_000m, dto.MonthlyRevenue);
    }

    [Fact]
    public async Task T4_Dashboard_revenue_sums_all_active_contracts()
    {
        var c = await _factory.AuthedClientAsync("dash-t4");

        // Create 2 contracts with different rents
        var prop1R = await c.PostAsJsonAsync("/api/v1/properties", new { address = "P1", city = "BA", province = "BA", propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null });
        var prop2R = await c.PostAsJsonAsync("/api/v1/properties", new { address = "P2", city = "BA", province = "BA", propertyType = "House", areaM2 = (decimal?)null, notes = (string?)null });
        prop1R.EnsureSuccessStatusCode(); prop2R.EnsureSuccessStatusCode();
        var p1 = (await prop1R.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
        var p2 = (await prop2R.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();

        var t1R = await c.PostAsJsonAsync("/api/v1/tenants", new { firstName = "A", lastName = "B", dni = "11111111", email = (string?)null, phone = (string?)null });
        var t2R = await c.PostAsJsonAsync("/api/v1/tenants", new { firstName = "C", lastName = "D", dni = "22222222", email = (string?)null, phone = (string?)null });
        t1R.EnsureSuccessStatusCode(); t2R.EnsureSuccessStatusCode();
        var t1 = (await t1R.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();
        var t2 = (await t2R.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("id").GetGuid();

        static object ContractBody(Guid pId, Guid tId, decimal rent) => new
        {
            propertyId = pId, appTenantId = tId,
            startDate = "2026-01-01", endDate = "2028-01-01",
            monthlyRent = rent, currency = "ARS",
            adjustmentType = "Manual", adjustmentFrequency = "Quarterly", dayOfMonth = 1,
            depositAmount = (decimal?)null, notes = (string?)null,
        };

        (await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(p1, t1, 200_000m))).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(p2, t2, 150_000m))).EnsureSuccessStatusCode();

        var r = await c.GetAsync("/api/v1/dashboard");
        var dto = await r.Content.ReadFromJsonAsync<DashboardDto>(JsonOpts);
        Assert.Equal(2, dto!.ActiveContractsCount);
        Assert.Equal(350_000m, dto.MonthlyRevenue);
    }

    [Fact]
    public async Task T5_Dashboard_cross_org_isolation()
    {
        var (_, _, c1) = await _factory.SetupContractWithTenantAsync("dash-t5a");
        var c2 = await _factory.AuthedClientAsync("dash-t5b");

        var r = await c2.GetAsync("/api/v1/dashboard");
        var dto = await r.Content.ReadFromJsonAsync<DashboardDto>(JsonOpts);
        Assert.Equal(0, dto!.ActiveContractsCount);
        Assert.Equal(0m, dto.MonthlyRevenue);
        Assert.Empty(dto.RecentTransactions);
    }

    // ── Me: GET /api/v1/me/contract & /me/transactions ───────────────────────

    [Fact]
    public async Task T6_Me_contract_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/me/contract");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T7_Me_contract_returns_404_when_no_appTenant_linked()
    {
        // Admin user has no AppTenant linked → 404
        var c = await _factory.AuthedClientAsync("me-t7");
        var r = await c.GetAsync("/api/v1/me/contract");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task T8_Invite_tenant_creates_user_and_me_contract_returns_contract()
    {
        var (contractId, tenantId, adminClient) = await _factory.SetupContractWithTenantAsync("me-t8");

        // Admin invites the tenant
        var inviteR = await adminClient.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        Assert.Equal(HttpStatusCode.OK, inviteR.StatusCode);
        var inviteBody = await inviteR.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOpts);
        var tempPassword = inviteBody.GetProperty("tempPassword").GetString()!;
        var tenantEmail = $"laura@me-t8.com";

        // Tenant logs in
        var tenantClient = _factory.CreateClient();
        var loginR = await tenantClient.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email = tenantEmail,
            password = tempPassword,
            organizationSlug = "me-t8",
        });
        Assert.Equal(HttpStatusCode.OK, loginR.StatusCode);
        var loginBody = await loginR.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOpts);
        var token = loginBody.GetProperty("token").GetString()!;
        tenantClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Tenant calls /me/contract
        var contractR = await tenantClient.GetAsync("/api/v1/me/contract");
        Assert.Equal(HttpStatusCode.OK, contractR.StatusCode);
        var contractDto = await contractR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);
        Assert.NotNull(contractDto);
        Assert.Equal(contractId, contractDto!.Id);
        Assert.Equal(ContractStatus.Active, contractDto.Status);
        Assert.Equal(300_000m, contractDto.MonthlyRent);
    }

    // Phase 0 regression — role authorization: an admin-only endpoint is reachable by staff
    // but forbidden (403) to an authenticated Tenant. Guards against privilege escalation.
    [Fact]
    public async Task Admin_endpoint_allows_staff_but_forbids_tenant()
    {
        var (_, tenantId, adminClient) = await _factory.SetupContractWithTenantAsync("role-t");

        // Staff reaches the admin-only endpoint.
        var adminResp = await adminClient.GetAsync("/api/v1/properties");
        Assert.Equal(HttpStatusCode.OK, adminResp.StatusCode);

        // Invite + tenant login to obtain a (valid, authenticated) Tenant token.
        var inviteR = await adminClient.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        var inviteBody = await inviteR.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOpts);
        var tempPassword = inviteBody.GetProperty("tempPassword").GetString()!;

        var tenantClient = _factory.CreateClient();
        var loginR = await tenantClient.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email = "laura@role-t.com",
            password = tempPassword,
            organizationSlug = "role-t",
        });
        loginR.EnsureSuccessStatusCode();
        var loginBody = await loginR.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOpts);
        var token = loginBody.GetProperty("token").GetString()!;
        tenantClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // The Tenant is authenticated (it can read /me/contract) but must NOT reach admin endpoints.
        var meResp = await tenantClient.GetAsync("/api/v1/me/contract");
        Assert.Equal(HttpStatusCode.OK, meResp.StatusCode);

        var forbiddenResp = await tenantClient.GetAsync("/api/v1/properties");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResp.StatusCode);
    }

    [Fact]
    public async Task T9_Me_transactions_returns_tenant_transactions()
    {
        var (contractId, tenantId, adminClient) = await _factory.SetupContractWithTenantAsync("me-t9");

        // Register a payment on the contract
        await adminClient.PostAsJsonAsync($"/api/v1/contracts/{contractId}/payments", new
        {
            amount = 300_000m,
            period = "2026-02-01",
            notes = "Pago febrero",
        });

        // Invite and login as tenant
        var inviteR = await adminClient.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        var inviteBody = await inviteR.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOpts);
        var tempPassword = inviteBody.GetProperty("tempPassword").GetString()!;

        var tenantClient = _factory.CreateClient();
        var loginR = await tenantClient.PostAsJsonAsync("/api/v1/auth/tenant-login", new
        {
            email = $"laura@me-t9.com",
            password = tempPassword,
            organizationSlug = "me-t9",
        });
        var loginBody = await loginR.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(JsonOpts);
        var token = loginBody.GetProperty("token").GetString()!;
        tenantClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var r = await tenantClient.GetAsync("/api/v1/me/transactions");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var txs = await r.Content.ReadFromJsonAsync<List<TransactionDto>>(JsonOpts);
        Assert.NotNull(txs);
        Assert.Single(txs!);
        Assert.Equal(TransactionType.Payment, txs![0].Type);
        Assert.Equal(300_000m, txs[0].Amount);
    }

    [Fact]
    public async Task T10_Invite_already_invited_tenant_returns_409()
    {
        var (_, tenantId, adminClient) = await _factory.SetupContractWithTenantAsync("me-t10");

        // First invite succeeds
        (await adminClient.PostAsync($"/api/v1/tenants/{tenantId}/invite", null)).EnsureSuccessStatusCode();

        // Second invite should fail
        var r2 = await adminClient.PostAsync($"/api/v1/tenants/{tenantId}/invite", null);
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }
}
