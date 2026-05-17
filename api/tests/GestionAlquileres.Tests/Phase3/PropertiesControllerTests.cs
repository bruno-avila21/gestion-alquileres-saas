using System.Net;
using System.Net.Http.Json;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Enums;

namespace GestionAlquileres.Tests.Phase3;

[Trait("Phase", "Phase3")]
public class PropertiesControllerTests : IClassFixture<Phase3ApiFactory>
{
    private readonly Phase3ApiFactory _factory;

    public PropertiesControllerTests(Phase3ApiFactory factory) => _factory = factory;

    private static readonly System.Text.Json.JsonSerializerOptions JsonOpts =
        new(System.Text.Json.JsonSerializerDefaults.Web)
        {
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };

    [Fact]
    public async Task T1_Get_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/properties");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T2_Post_without_auth_returns_401()
    {
        var c = _factory.CreateClient();
        var r = await c.PostAsJsonAsync("/api/v1/properties", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T3_Create_returns_201_with_correct_data()
    {
        var c = await _factory.AuthedClientAsync("prop-t3");
        var r = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Av. Corrientes 1234",
            city = "Buenos Aires",
            province = "CABA",
            propertyType = "Apartment",
            areaM2 = 65.5m,
            notes = (string?)null,
        });

        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);
        Assert.NotNull(dto);
        Assert.Equal("Av. Corrientes 1234", dto!.Address);
        Assert.Equal("Buenos Aires", dto.City);
        Assert.Equal(PropertyType.Apartment, dto.PropertyType);
        Assert.Equal(65.5m, dto.AreaM2);
        Assert.True(dto.IsActive);
    }

    [Fact]
    public async Task T4_GetAll_returns_only_own_org_properties()
    {
        var c1 = await _factory.AuthedClientAsync("prop-t4a");
        var c2 = await _factory.AuthedClientAsync("prop-t4b");

        await c1.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Calle Falsa 123", city = "Rosario", province = "Santa Fe",
            propertyType = "House", areaM2 = (decimal?)null, notes = (string?)null,
        });

        var r2 = await c2.GetAsync("/api/v1/properties");
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        var list = await r2.Content.ReadFromJsonAsync<List<PropertyDto>>(JsonOpts);
        Assert.NotNull(list);
        Assert.Empty(list!); // org2 should see 0 properties
    }

    [Fact]
    public async Task T5_GetById_returns_property()
    {
        var c = await _factory.AuthedClientAsync("prop-t5");
        var createR = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Sarmiento 456", city = "Córdoba", province = "Córdoba",
            propertyType = "Commercial", areaM2 = 100m, notes = "Local comercial",
        });
        var created = await createR.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);

        var r = await c.GetAsync($"/api/v1/properties/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var dto = await r.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);
        Assert.Equal(created.Id, dto!.Id);
        Assert.Equal("Local comercial", dto.Notes);
    }

    [Fact]
    public async Task T6_GetById_cross_org_returns_404()
    {
        var c1 = await _factory.AuthedClientAsync("prop-t6a");
        var c2 = await _factory.AuthedClientAsync("prop-t6b");

        var createR = await c1.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Belgrano 789", city = "Mendoza", province = "Mendoza",
            propertyType = "House", areaM2 = (decimal?)null, notes = (string?)null,
        });
        var created = await createR.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);

        // Org2 tries to access Org1's property — should get 404 (filtered by global query)
        var r = await c2.GetAsync($"/api/v1/properties/{created!.Id}");
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
    }

    [Fact]
    public async Task T7_Create_with_invalid_data_returns_400()
    {
        var c = await _factory.AuthedClientAsync("prop-t7");
        var r = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "",          // invalid: empty
            city = "Buenos Aires",
            province = "CABA",
            propertyType = "Apartment",
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task T8_Update_modifies_property()
    {
        var c = await _factory.AuthedClientAsync("prop-t8");
        var createR = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Original St 1", city = "Tucumán", province = "Tucumán",
            propertyType = "House", areaM2 = (decimal?)null, notes = (string?)null,
        });
        var created = await createR.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);

        var updateR = await c.PutAsJsonAsync($"/api/v1/properties/{created!.Id}", new
        {
            address = "Updated St 99",
            city = "Tucumán",
            province = "Tucumán",
            propertyType = "Apartment",
            areaM2 = 80m,
            notes = "Actualizado",
            isActive = true,
        });

        Assert.Equal(HttpStatusCode.OK, updateR.StatusCode);
        var updated = await updateR.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);
        Assert.Equal("Updated St 99", updated!.Address);
        Assert.Equal(PropertyType.Apartment, updated.PropertyType);
        Assert.Equal(80m, updated.AreaM2);
    }

    [Fact]
    public async Task T9_Delete_soft_deletes_property()
    {
        var c = await _factory.AuthedClientAsync("prop-t9");
        var createR = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "ToDelete 1", city = "Salta", province = "Salta",
            propertyType = "Land", areaM2 = (decimal?)null, notes = (string?)null,
        });
        var created = await createR.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);

        var deleteR = await c.DeleteAsync($"/api/v1/properties/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteR.StatusCode);

        // Verify IsActive = false via GET
        var getR = await c.GetAsync($"/api/v1/properties/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getR.StatusCode);
        var dto = await getR.Content.ReadFromJsonAsync<PropertyDto>(JsonOpts);
        Assert.False(dto!.IsActive);
    }
}
