using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionAlquileres.Application.Features.Listings.DTOs;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Application.Features.PropertyPhotos.DTOs;
using GestionAlquileres.Application.Features.Public.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Tests.Phase7;

namespace GestionAlquileres.Tests.Phase11;

/// <summary>
/// Publicaciones (Listing), fotos y sitio público por slug. Usa Phase7ApiFactory porque trae
/// storage local para las fotos.
/// </summary>
[Trait("Phase", "Phase11")]
public class ListingsAndPublicSiteTests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public ListingsAndPublicSiteTests(Phase7ApiFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static async Task<PropertyDto> CreatePropertyAsync(HttpClient c, string address = "Av. Salvador María del Carril 3100")
    {
        var r = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address,
            city = "Buenos Aires",
            province = "CABA",
            propertyType = "Apartment",
            areaM2 = 36m,
            details = new
            {
                neighborhood = "Villa Pueyrredón",
                code = "PAP8664371",
                rooms = 2,
                bedrooms = 1,
                bathrooms = 1,
                coveredAreaM2 = 34m,
                suitableForCredit = true,
                features = new[] { "Gas natural", "Apto mascotas", " gas natural " },
            },
        }, Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<PropertyDto>(Json))!;
    }

    private static async Task<ListingDto> CreateListingAsync(HttpClient c, Guid propertyId, string status, string operation = "Rent", decimal price = 800_000m)
    {
        var r = await c.PostAsJsonAsync("/api/v1/listings", new
        {
            propertyId,
            operationType = operation,
            price,
            currency = "ARS",
            title = "Departamento dos ambientes en Villa Pueyrredón",
            status,
        }, Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ListingDto>(Json))!;
    }

    [Fact]
    public async Task T1_Property_details_round_trip_and_features_are_deduplicated()
    {
        var c = await _factory.AuthedClientAsync("p11-t1");
        var p = await CreatePropertyAsync(c);

        Assert.Equal("Villa Pueyrredón", p.Neighborhood);
        Assert.Equal(2, p.Rooms);
        Assert.True(p.SuitableForCredit);
        Assert.Equal(new[] { "Gas natural", "Apto mascotas" }, p.Features);
    }

    [Fact]
    public async Task T2_Listings_require_auth()
    {
        var c = _factory.CreateClient();
        var r = await c.GetAsync("/api/v1/listings");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task T3_Draft_is_invisible_publicly_and_published_appears_with_facets()
    {
        var c = await _factory.AuthedClientAsync("p11-t3");
        var p = await CreatePropertyAsync(c);
        var draft = await CreateListingAsync(c, p.Id, "Draft");

        var anon = _factory.CreateClient();
        var empty = await anon.GetFromJsonAsync<PublicListingSearchResultDto>("/api/v1/public/p11-t3/listings", Json);
        Assert.Equal(0, empty!.Total);

        var detail404 = await anon.GetAsync($"/api/v1/public/p11-t3/listings/{draft.Id}");
        Assert.Equal(HttpStatusCode.NotFound, detail404.StatusCode);

        var upd = await c.PutAsJsonAsync($"/api/v1/listings/{draft.Id}", new
        {
            operationType = "Rent", price = 850_000m, currency = "ARS", title = draft.Title, isFeatured = true, status = "Published",
        }, Json);
        Assert.Equal(HttpStatusCode.OK, upd.StatusCode);
        var published = (await upd.Content.ReadFromJsonAsync<ListingDto>(Json))!;
        Assert.NotNull(published.PublishedAt);

        var result = await anon.GetFromJsonAsync<PublicListingSearchResultDto>("/api/v1/public/P11-T3/listings?operation=Rent&type=Apartment", Json);
        Assert.Equal(1, result!.Total);
        var card = result.Items.Single();
        Assert.Equal(850_000m, card.Price);
        Assert.Equal("PAP8664371", card.Code);
        Assert.Contains(result.Facets.PropertyTypes, f => f.Value == "Apartment" && f.Count == 1);
        Assert.Contains(result.Facets.Features, f => f.Value == "Gas natural" && f.Count == 1);
        Assert.Contains(result.Facets.Rooms, f => f.Value == "2");

        var filteredOut = await anon.GetFromJsonAsync<PublicListingSearchResultDto>("/api/v1/public/p11-t3/listings?minRooms=3", Json);
        Assert.Equal(0, filteredOut!.Total);

        var detail = await anon.GetFromJsonAsync<PublicListingDetailDto>($"/api/v1/public/p11-t3/listings/{draft.Id}", Json);
        Assert.Equal("Villa Pueyrredón", detail!.Neighborhood);
        Assert.Contains("Apto mascotas", detail.Features);
    }

    [Fact]
    public async Task T4_Public_site_is_isolated_by_slug_and_unknown_slug_is_404()
    {
        var a = await _factory.AuthedClientAsync("p11-t4a");
        var pa = await CreatePropertyAsync(a);
        await CreateListingAsync(a, pa.Id, "Published", "Sale", 120_000m);

        var b = await _factory.AuthedClientAsync("p11-t4b");

        var anon = _factory.CreateClient();
        var siteA = await anon.GetFromJsonAsync<PublicListingSearchResultDto>("/api/v1/public/p11-t4a/listings", Json);
        var siteB = await anon.GetFromJsonAsync<PublicListingSearchResultDto>("/api/v1/public/p11-t4b/listings", Json);
        Assert.Equal(1, siteA!.Total);
        Assert.Equal(0, siteB!.Total);

        // Org B's admin listing endpoint must not see A's listing either.
        var bListings = await b.GetFromJsonAsync<List<ListingDto>>("/api/v1/listings", Json);
        Assert.Empty(bListings!);

        var unknown = await anon.GetAsync("/api/v1/public/no-existe/listings");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);

        var org = await anon.GetFromJsonAsync<PublicOrganizationDto>("/api/v1/public/p11-t4a", Json);
        Assert.Equal("p11-t4a", org!.Slug);
    }

    [Fact]
    public async Task T5_Second_published_listing_for_same_operation_is_rejected()
    {
        var c = await _factory.AuthedClientAsync("p11-t5");
        var p = await CreatePropertyAsync(c);
        await CreateListingAsync(c, p.Id, "Published", "Sale", 100_000m);

        var dup = await c.PostAsJsonAsync("/api/v1/listings", new
        {
            propertyId = p.Id, operationType = "Sale", price = 110_000m, currency = "USD", title = "Otra", status = "Published",
        }, Json);
        Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);

        // A different operation on the same property is fine (sale + rent at once).
        var rent = await CreateListingAsync(c, p.Id, "Published", "Rent");
        Assert.Equal(ListingStatus.Published, rent.Status);
    }

    [Fact]
    public async Task T6_Photos_upload_serve_publicly_only_when_published_and_delete()
    {
        var c = await _factory.AuthedClientAsync("p11-t6");
        var p = await CreatePropertyAsync(c);

        using var form = new MultipartFormDataContent();
        var bytes = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3, 4 });
        bytes.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(bytes, "file", "frente.jpg");
        var up = await c.PostAsync($"/api/v1/properties/{p.Id}/photos", form);
        Assert.Equal(HttpStatusCode.Created, up.StatusCode);
        var photo = (await up.Content.ReadFromJsonAsync<PropertyPhotoDto>(Json))!;
        Assert.True(photo.IsCover);
        Assert.Equal($"/api/v1/public/p11-t6/photos/{photo.Id}", photo.Url);

        // Not published yet: the photo must not be served to the public.
        var anon = _factory.CreateClient();
        var hidden = await anon.GetAsync(photo.Url);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var listing = await CreateListingAsync(c, p.Id, "Published");
        var served = await anon.GetAsync(photo.Url);
        Assert.Equal(HttpStatusCode.OK, served.StatusCode);
        Assert.Equal("image/jpeg", served.Content.Headers.ContentType!.MediaType);

        var card = (await anon.GetFromJsonAsync<PublicListingSearchResultDto>("/api/v1/public/p11-t6/listings", Json))!.Items.Single();
        Assert.Equal(photo.Url, card.CoverPhotoUrl);
        var detail = await anon.GetFromJsonAsync<PublicListingDetailDto>($"/api/v1/public/p11-t6/listings/{listing.Id}", Json);
        Assert.Single(detail!.PhotoUrls);

        // Active content is refused.
        using var svgForm = new MultipartFormDataContent();
        var svg = new ByteArrayContent(new byte[] { 1 });
        svg.Headers.ContentType = new MediaTypeHeaderValue("image/svg+xml");
        svgForm.Add(svg, "file", "x.svg");
        var bad = await c.PostAsync($"/api/v1/properties/{p.Id}/photos", svgForm);
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);

        var del = await c.DeleteAsync($"/api/v1/properties/{p.Id}/photos/{photo.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        var gone = await anon.GetAsync(photo.Url);
        Assert.Equal(HttpStatusCode.NotFound, gone.StatusCode);
    }
}
