using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.AppTenants.DTOs;

namespace GestionAlquileres.Tests.Phase3;

[Trait("Phase", "Phase3")]
public class AppTenantsControllerTests : IClassFixture<Phase3ApiFactory>
{
    private readonly Phase3ApiFactory _factory;

    public AppTenantsControllerTests(Phase3ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web);

    [Fact]
    public async Task T1_Create_tenant_returns_201()
    {
        var c = await _factory.AuthedClientAsync("tnt-t1");
        var r = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Juan",
            lastName = "García",
            dni = "12345678",
            email = "juan@example.com",
            phone = "+54 11 1234-5678",
        });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal("Juan", dto!.FirstName);
        Assert.Equal("12345678", dto.Dni);
        Assert.Equal("juan@example.com", dto.Email);
        Assert.Null(dto.UserId);
    }

    [Fact]
    public async Task T2_GetAll_returns_own_tenants()
    {
        var c = await _factory.AuthedClientAsync("tnt-t2");
        await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Ana", lastName = "López", dni = "87654321",
            email = (string?)null, phone = (string?)null,
        });

        var r = await c.GetAsync("/api/v1/tenants");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var list = await r.Content.ReadFromJsonAsync<List<AppTenantDto>>(JsonOpts);
        Assert.Single(list!);
    }

    [Fact]
    public async Task T3_Duplicate_DNI_returns_400()
    {
        var c = await _factory.AuthedClientAsync("tnt-t3");
        var body = new
        {
            firstName = "Pedro", lastName = "Martínez", dni = "11112222",
            email = (string?)null, phone = (string?)null,
        };
        await c.PostAsJsonAsync("/api/v1/tenants", body);
        var r2 = await c.PostAsJsonAsync("/api/v1/tenants", body);

        Assert.Equal(HttpStatusCode.Conflict, r2.StatusCode);
    }

    [Fact]
    public async Task T4_Update_modifies_tenant()
    {
        var c = await _factory.AuthedClientAsync("tnt-t4");
        var createR = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Laura", lastName = "Fernández", dni = "33334444",
            email = (string?)null, phone = (string?)null,
        });
        var created = await createR.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);

        var updateR = await c.PutAsJsonAsync($"/api/v1/tenants/{created!.Id}", new
        {
            firstName = "Laura Updated",
            lastName = "Fernández",
            dni = "33334444",
            email = "laura@example.com",
            phone = "1234",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.OK, updateR.StatusCode);
        var updated = await updateR.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);
        Assert.Equal("Laura Updated", updated!.FirstName);
        Assert.Equal("laura@example.com", updated.Email);
    }

    [Fact]
    public async Task T5_Delete_soft_deletes_tenant()
    {
        var c = await _factory.AuthedClientAsync("tnt-t5");
        var createR = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Carlos", lastName = "Ruiz", dni = "55556666",
            email = (string?)null, phone = (string?)null,
        });
        var created = await createR.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);

        var deleteR = await c.DeleteAsync($"/api/v1/tenants/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteR.StatusCode);

        var getR = await c.GetAsync($"/api/v1/tenants/{created.Id}");
        var dto = await getR.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);
        Assert.False(dto!.IsActive);
    }

    [Fact]
    public async Task T6_Invite_creates_user_and_returns_temp_password()
    {
        var c = await _factory.AuthedClientAsync("tnt-t6");
        var createR = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "María", lastName = "González", dni = "77778888",
            email = "maria@example.com", phone = (string?)null,
        });
        var created = await createR.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);

        var inviteR = await c.PostAsync($"/api/v1/tenants/{created!.Id}/invite", null);
        Assert.Equal(HttpStatusCode.OK, inviteR.StatusCode);

        var result = await inviteR.Content.ReadFromJsonAsync<InviteTenantResult>(JsonOpts);
        Assert.NotNull(result);
        Assert.NotEmpty(result!.TempPassword);
        Assert.NotNull(result.Tenant.UserId);
    }

    /// <summary>
    /// Re-invitar ahora regenera la contraseña temporal en vez de rechazar con 409.
    ///
    /// El contrato anterior ("este inquilino ya tiene acceso al portal") dejaba sin ninguna vía
    /// para rotar una credencial perdida o filtrada: había que tocar la base a mano. El detalle
    /// del comportamiento nuevo está en Phase7/ChangePasswordTests.
    /// </summary>
    [Fact]
    public async Task T7_Invite_twice_regenerates_the_temp_password()
    {
        var c = await _factory.AuthedClientAsync("tnt-t7");
        var createR = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Roberto", lastName = "Díaz", dni = "99990000",
            email = "roberto@example.com", phone = (string?)null,
        });
        var created = await createR.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);

        var r1 = await c.PostAsync($"/api/v1/tenants/{created!.Id}/invite", null);
        r1.EnsureSuccessStatusCode();
        var primera = (await r1.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("tempPassword").GetString();

        var r2 = await c.PostAsync($"/api/v1/tenants/{created.Id}/invite", null);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var segunda = (await r2.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("tempPassword").GetString();

        Assert.NotEqual(primera, segunda);
    }

    [Fact]
    public async Task T8_Invite_without_email_returns_400()
    {
        var c = await _factory.AuthedClientAsync("tnt-t8");
        var createR = await c.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Silvia", lastName = "Torres", dni = "11223344",
            email = (string?)null, phone = (string?)null,
        });
        var created = await createR.Content.ReadFromJsonAsync<AppTenantDto>(JsonOpts);

        var r = await c.PostAsync($"/api/v1/tenants/{created!.Id}/invite", null);
        Assert.Equal(HttpStatusCode.Conflict, r.StatusCode);
    }
}
