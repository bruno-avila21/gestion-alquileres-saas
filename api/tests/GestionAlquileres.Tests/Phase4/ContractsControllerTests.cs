using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.AppTenants.DTOs;
using GestionAlquileres.Application.Features.Contracts.DTOs;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Tests.Phase4;

[Trait("Phase", "Phase4")]
public class ContractsControllerTests : IClassFixture<Phase4ApiFactory>
{
    private readonly Phase4ApiFactory _factory;

    public ContractsControllerTests(Phase4ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    private static object ContractBody(Guid propertyId, Guid tenantId,
        decimal rent = 150_000m, int dayOfMonth = 1) =>
        new
        {
            propertyId,
            appTenantId = tenantId,
            startDate = "2026-01-01",
            endDate = "2028-01-01",
            monthlyRent = rent,
            currency = "ARS",
            adjustmentType = "ICL",
            adjustmentFrequency = "Quarterly",
            dayOfMonth,
            depositAmount = (decimal?)null,
            notes = (string?)null,
        };

    private async Task<(PropertyDto property, AppTenantDto tenant)> SetupAsync(HttpClient client)
    {
        var propResp = await client.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "San Martín 100", city = "Buenos Aires", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        propResp.EnsureSuccessStatusCode();
        var property = await propResp.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);

        var tenantResp = await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Juan", lastName = "Pérez", dni = "12345678",
            email = (string?)null, phone = (string?)null,
        });
        tenantResp.EnsureSuccessStatusCode();
        var tenant = await tenantResp.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);

        return (property!, tenant!);
    }

    [Fact]
    public async Task T1_Get_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/contracts");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T2_Post_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/contracts", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T3_Create_returns_201_with_correct_data()
    {
        var c = await _factory.AuthedClientAsync("cont-t3");
        var (property, tenant) = await SetupAsync(c);

        var r = await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal(property.Id, dto!.PropertyId);
        Assert.Equal(tenant.Id, dto.AppTenantId);
        Assert.Equal(150_000m, dto.MonthlyRent);
        Assert.Equal(ContractStatus.Active, dto.Status);
        Assert.Equal(AdjustmentType.ICL, dto.AdjustmentType);
        Assert.Equal(1, dto.DayOfMonth);
        Assert.Equal("San Martín 100", dto.PropertyAddress);
        Assert.Equal("Juan Pérez", dto.AppTenantFullName);
    }

    [Fact]
    public async Task T4_GetAll_returns_only_own_org_contracts()
    {
        var c1 = await _factory.AuthedClientAsync("cont-t4a");
        var c2 = await _factory.AuthedClientAsync("cont-t4b");

        var (property, tenant) = await SetupAsync(c1);
        await c1.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));

        var r2 = await c2.GetAsync("/api/v1/contracts");
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var list = await r2.Content.ReadFromJsonAsync<List<ContractDto>>(JsonOpts);
        Assert.NotNull(list);
        Assert.Empty(list!);
    }

    [Fact]
    public async Task T5_GetById_returns_contract()
    {
        var c = await _factory.AuthedClientAsync("cont-t5");
        var (property, tenant) = await SetupAsync(c);

        var createR = await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id, rent: 200_000m));
        var created = await createR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);

        var r = await c.GetAsync($"/api/v1/contracts/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);
        Assert.Equal(created.Id, dto!.Id);
        Assert.Equal(200_000m, dto.MonthlyRent);
    }

    [Fact]
    public async Task T6_GetById_cross_org_returns_404()
    {
        var c1 = await _factory.AuthedClientAsync("cont-t6a");
        var c2 = await _factory.AuthedClientAsync("cont-t6b");

        var (property, tenant) = await SetupAsync(c1);
        var createR = await c1.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));
        var created = await createR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);

        var r = await c2.GetAsync($"/api/v1/contracts/{created!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task T7_Create_with_invalid_data_returns_400()
    {
        var c = await _factory.AuthedClientAsync("cont-t7");
        var (property, tenant) = await SetupAsync(c);

        // MonthlyRent = 0 should fail validation
        var r = await c.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = property.Id,
            appTenantId = tenant.Id,
            startDate = "2026-01-01",
            endDate = "2028-01-01",
            monthlyRent = 0m,
            currency = "ARS",
            adjustmentType = "ICL",
            adjustmentFrequency = "Quarterly",
            dayOfMonth = 1,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task T8_Create_with_invalid_dates_returns_400()
    {
        var c = await _factory.AuthedClientAsync("cont-t8");
        var (property, tenant) = await SetupAsync(c);

        // EndDate before StartDate
        var r = await c.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = property.Id,
            appTenantId = tenant.Id,
            startDate = "2028-01-01",
            endDate = "2026-01-01",
            monthlyRent = 100_000m,
            currency = "ARS",
            adjustmentType = "IPC",
            adjustmentFrequency = "Monthly",
            dayOfMonth = 5,
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task T9_Update_modifies_contract()
    {
        var c = await _factory.AuthedClientAsync("cont-t9");
        var (property, tenant) = await SetupAsync(c);

        var createR = await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));
        var created = await createR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);

        var updateR = await c.PutAsJsonAsync($"/api/v1/contracts/{created!.Id}", new
        {
            propertyId = property.Id,
            appTenantId = tenant.Id,
            startDate = "2026-03-01",
            endDate = "2029-03-01",
            monthlyRent = 250_000m,
            currency = "ARS",
            adjustmentType = "ICL",
            adjustmentFrequency = "Annual",
            dayOfMonth = 10,
            depositAmount = 500_000m,
            notes = "Actualizado",
        });

        Assert.Equal(HttpStatusCode.OK, updateR.StatusCode);
        var updated = await updateR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);
        Assert.Equal(250_000m, updated!.MonthlyRent);
        Assert.Equal(AdjustmentFrequency.Annual, updated.AdjustmentFrequency);
        Assert.Equal(10, updated.DayOfMonth);
        Assert.Equal(500_000m, updated.DepositAmount);
        Assert.Equal("Actualizado", updated.Notes);
    }

    [Fact]
    public async Task T10_Terminate_changes_status_to_terminated()
    {
        var c = await _factory.AuthedClientAsync("cont-t10");
        var (property, tenant) = await SetupAsync(c);

        var createR = await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));
        var created = await createR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);
        Assert.Equal(ContractStatus.Active, created!.Status);

        var termR = await c.PostAsJsonAsync($"/api/v1/contracts/{created.Id}/terminate",
            new { notes = "Rescisión acordada" });

        Assert.Equal(HttpStatusCode.OK, termR.StatusCode);
        var terminated = await termR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);
        Assert.Equal(ContractStatus.Terminated, terminated!.Status);
        Assert.NotNull(terminated.TerminatedAt);
        Assert.Equal("Rescisión acordada", terminated.Notes);
    }

    [Fact]
    public async Task T11_Terminate_already_terminated_returns_409()
    {
        var c = await _factory.AuthedClientAsync("cont-t11");
        var (property, tenant) = await SetupAsync(c);

        var createR = await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));
        var created = await createR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);

        await c.PostAsJsonAsync($"/api/v1/contracts/{created!.Id}/terminate", new { notes = (string?)null });

        // Second termination should fail with 409
        var r2 = await c.PostAsJsonAsync($"/api/v1/contracts/{created.Id}/terminate", new { notes = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task T12_Create_with_nonexistent_property_returns_409()
    {
        var c = await _factory.AuthedClientAsync("cont-t12");
        var (_, tenant) = await SetupAsync(c);

        var r = await c.PostAsJsonAsync("/api/v1/contracts",
            ContractBody(Guid.NewGuid(), tenant.Id));
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }

    [Fact]
    public async Task T14_Create_overlapping_active_contract_on_same_property_returns_409()
    {
        var c = await _factory.AuthedClientAsync("cont-t14");
        var (property, tenant) = await SetupAsync(c);

        // First contract 2026-01-01 → 2028-01-01
        var first = await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second contract on the same property overlapping (2027 falls inside the first range)
        var second = await c.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = property.Id,
            appTenantId = tenant.Id,
            startDate = "2027-06-01",
            endDate = "2029-06-01",
            monthlyRent = 180_000m,
            currency = "ARS",
            adjustmentType = "IPC",
            adjustmentFrequency = "Monthly",
            dayOfMonth = 1,
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task T15_Create_non_overlapping_contract_on_same_property_succeeds()
    {
        var c = await _factory.AuthedClientAsync("cont-t15");
        var (property, tenant) = await SetupAsync(c);

        // First contract 2026-01-01 → 2028-01-01 (left active)
        var first = await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Second contract, same property, range starts after the first ends → no overlap, both active
        var second = await c.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = property.Id,
            appTenantId = tenant.Id,
            startDate = "2028-02-01",
            endDate = "2030-02-01",
            monthlyRent = 180_000m,
            currency = "ARS",
            adjustmentType = "IPC",
            adjustmentFrequency = "Monthly",
            dayOfMonth = 1,
        });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task T13_List_filter_by_status_works()
    {
        var c = await _factory.AuthedClientAsync("cont-t13");
        var (property, tenant) = await SetupAsync(c);

        var createR = await c.PostAsJsonAsync("/api/v1/contracts", ContractBody(property.Id, tenant.Id));
        var created = await createR.Content.ReadFromJsonAsync<ContractDto>(JsonOpts);

        await c.PostAsJsonAsync($"/api/v1/contracts/{created!.Id}/terminate", new { notes = (string?)null });

        var activeR = await c.GetAsync("/api/v1/contracts?status=Active");
        var activeList = await activeR.Content.ReadFromJsonAsync<List<ContractDto>>(JsonOpts);
        Assert.DoesNotContain(activeList!, x => x.Id == created.Id);

        var termR = await c.GetAsync("/api/v1/contracts?status=Terminated");
        var termList = await termR.Content.ReadFromJsonAsync<List<ContractDto>>(JsonOpts);
        Assert.Contains(termList!, x => x.Id == created.Id);
    }
}
