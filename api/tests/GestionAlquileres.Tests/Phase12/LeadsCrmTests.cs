using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionAlquileres.Application.Common.DTOs;
using GestionAlquileres.Application.Features.Leads.DTOs;
using GestionAlquileres.Application.Features.Listings.DTOs;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Tests.Phase7;

namespace GestionAlquileres.Tests.Phase12;

/// <summary>
/// CRM de consultas (leads), bloque A3: alta pública por slug (con honeypot), y gestión desde el
/// panel (estados, notas, resumen). Usa Phase7ApiFactory por el mismo motivo que Phase11: sólo
/// necesita el host completo con auth + tenant, sin storage real.
/// </summary>
[Trait("Phase", "Phase12")]
public class LeadsCrmTests : IClassFixture<Phase7ApiFactory>
{
    private readonly Phase7ApiFactory _factory;
    public LeadsCrmTests(Phase7ApiFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private record IdResponse(Guid Id);

    private static async Task<PropertyDto> CreatePropertyAsync(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Av. Rivadavia 5000",
            city = "Buenos Aires",
            province = "CABA",
            propertyType = "Apartment",
            areaM2 = (decimal?)null,
            notes = (string?)null,
        }, Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<PropertyDto>(Json))!;
    }

    private static async Task<ListingDto> CreatePublishedListingAsync(HttpClient c, Guid propertyId)
    {
        var r = await c.PostAsJsonAsync("/api/v1/listings", new
        {
            propertyId,
            operationType = "Rent",
            price = 500_000m,
            currency = "ARS",
            title = "Depto en Caballito",
            status = "Published",
        }, Json);
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
        return (await r.Content.ReadFromJsonAsync<ListingDto>(Json))!;
    }

    [Fact]
    public async Task T1_Public_lead_with_valid_listing_is_created_and_visible_to_admin()
    {
        var c = await _factory.AuthedClientAsync("p12-t1");
        var p = await CreatePropertyAsync(c);
        var listing = await CreatePublishedListingAsync(c, p.Id);

        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync($"/api/v1/public/p12-t1/leads", new
        {
            name = "Juana Pérez",
            email = "juana@example.com",
            phone = (string?)null,
            message = "Me interesa esta propiedad, ¿sigue disponible?",
            listingId = listing.Id,
            website = "",
        }, Json);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var created = (await resp.Content.ReadFromJsonAsync<IdResponse>(Json))!;
        Assert.NotEqual(Guid.Empty, created.Id);

        var page = await c.GetFromJsonAsync<PagedResult<LeadDto>>("/api/v1/leads", Json);
        var lead = Assert.Single(page!.Items);
        Assert.Equal("Juana Pérez", lead.Name);
        Assert.Equal("Website", lead.Source.ToString());
        Assert.Equal("New", lead.Status.ToString());
        Assert.Equal(listing.Id, lead.ListingId);
        Assert.Equal(p.Id, lead.PropertyId);
        Assert.Equal("Depto en Caballito", lead.PropertyTitle);
    }

    [Fact]
    public async Task T2_Honeypot_is_silently_dropped()
    {
        var c = await _factory.AuthedClientAsync("p12-t2");

        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync("/api/v1/public/p12-t2/leads", new
        {
            name = "Bot",
            email = "bot@example.com",
            message = "Spam",
            website = "http://spam.example.com",
        }, Json);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var page = await c.GetFromJsonAsync<PagedResult<LeadDto>>("/api/v1/leads", Json);
        Assert.Empty(page!.Items);
    }

    [Fact]
    public async Task T3_Public_lead_without_email_or_phone_is_rejected()
    {
        _ = await _factory.AuthedClientAsync("p12-t3");
        var anon = _factory.CreateClient();

        var resp = await anon.PostAsJsonAsync("/api/v1/public/p12-t3/leads", new
        {
            name = "Sin Contacto",
            message = "Consulto por la propiedad",
            website = "",
        }, Json);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task T4_Lead_is_isolated_by_organization()
    {
        var a = await _factory.AuthedClientAsync("p12-t4a");
        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync("/api/v1/public/p12-t4a/leads", new
        {
            name = "Cliente A",
            email = "a@example.com",
            message = "Consulta general",
            website = "",
        }, Json);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var created = (await resp.Content.ReadFromJsonAsync<IdResponse>(Json))!;

        var b = await _factory.AuthedClientAsync("p12-t4b");
        var bList = await b.GetFromJsonAsync<PagedResult<LeadDto>>("/api/v1/leads", Json);
        Assert.Empty(bList!.Items);

        var bGetById = await b.GetAsync($"/api/v1/leads/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, bGetById.StatusCode);

        var aGetById = await a.GetAsync($"/api/v1/leads/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, aGetById.StatusCode);
    }

    [Fact]
    public async Task T5_Moving_to_Lost_requires_a_reason()
    {
        var c = await _factory.AuthedClientAsync("p12-t5");
        var create = await c.PostAsJsonAsync("/api/v1/leads", new
        {
            name = "Carga Manual",
            email = "manual@example.com",
            phone = (string?)null,
            message = "Llamó por teléfono preguntando por alquileres",
            listingId = (Guid?)null,
        }, Json);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var lead = (await create.Content.ReadFromJsonAsync<LeadDto>(Json))!;
        Assert.Equal("Manual", lead.Source.ToString());

        var withoutReason = await c.PatchAsJsonAsync($"/api/v1/leads/{lead.Id}/status", new
        {
            status = "Lost",
            lostReason = (string?)null,
        }, Json);
        // FluentValidation (400), not the handler's BusinessException (409): the ValidationBehavior
        // pipeline rejects the request before ChangeLeadStatusCommandHandler ever runs.
        Assert.Equal(HttpStatusCode.BadRequest, withoutReason.StatusCode);

        var beforeLastContact = lead.LastContactAt;
        var withReason = await c.PatchAsJsonAsync($"/api/v1/leads/{lead.Id}/status", new
        {
            status = "Lost",
            lostReason = "No contestó los llamados",
        }, Json);
        Assert.Equal(HttpStatusCode.OK, withReason.StatusCode);
        var updated = (await withReason.Content.ReadFromJsonAsync<LeadDto>(Json))!;
        Assert.Equal("Lost", updated.Status.ToString());
        Assert.Equal("No contestó los llamados", updated.LostReason);
        Assert.NotNull(updated.LastContactAt);
        Assert.NotEqual(beforeLastContact, updated.LastContactAt);
    }

    [Fact]
    public async Task T6_Adding_a_note_bumps_LastContactAt_and_notes_count()
    {
        var c = await _factory.AuthedClientAsync("p12-t6");
        var create = await c.PostAsJsonAsync("/api/v1/leads", new
        {
            name = "Nota Test",
            email = "nota@example.com",
            phone = (string?)null,
            message = "Consulta general",
            listingId = (Guid?)null,
        }, Json);
        var lead = (await create.Content.ReadFromJsonAsync<LeadDto>(Json))!;
        Assert.Equal(0, lead.NotesCount);
        Assert.Null(lead.LastContactAt);

        var noteResp = await c.PostAsJsonAsync($"/api/v1/leads/{lead.Id}/notes", new { text = "Lo llamé, pidió llamar mañana." }, Json);
        Assert.Equal(HttpStatusCode.Created, noteResp.StatusCode);
        var note = (await noteResp.Content.ReadFromJsonAsync<LeadNoteDto>(Json))!;
        Assert.Equal("Lo llamé, pidió llamar mañana.", note.Text);
        Assert.False(string.IsNullOrWhiteSpace(note.CreatedByName));

        var detail = await c.GetFromJsonAsync<LeadDetailDto>($"/api/v1/leads/{lead.Id}", Json);
        Assert.Equal(1, detail!.NotesCount);
        Assert.NotNull(detail.LastContactAt);
        Assert.Single(detail.Notes);
        Assert.Equal(note.Id, detail.Notes[0].Id);
    }

    [Fact]
    public async Task T7_Summary_counts_by_status()
    {
        var c = await _factory.AuthedClientAsync("p12-t7");

        async Task<LeadDto> CreateManual(string name)
        {
            var r = await c.PostAsJsonAsync("/api/v1/leads", new
            {
                name, email = $"{name}@example.com", phone = (string?)null,
                message = "Consulta", listingId = (Guid?)null,
            }, Json);
            return (await r.Content.ReadFromJsonAsync<LeadDto>(Json))!;
        }

        var l1 = await CreateManual("uno");
        var l2 = await CreateManual("dos");
        await CreateManual("tres");

        var toContacted = await c.PatchAsJsonAsync($"/api/v1/leads/{l1.Id}/status", new { status = "Contacted", lostReason = (string?)null }, Json);
        Assert.Equal(HttpStatusCode.OK, toContacted.StatusCode);

        var toLost = await c.PatchAsJsonAsync($"/api/v1/leads/{l2.Id}/status", new { status = "Lost", lostReason = "No responde" }, Json);
        Assert.Equal(HttpStatusCode.OK, toLost.StatusCode);

        var summary = await c.GetFromJsonAsync<LeadSummaryDto>("/api/v1/leads/summary", Json);
        Assert.Equal(3, summary!.Total);
        Assert.Equal(1, summary.ByStatus["New"]);
        Assert.Equal(1, summary.ByStatus["Contacted"]);
        Assert.Equal(1, summary.ByStatus["Lost"]);
        Assert.Equal(0, summary.ByStatus["Won"]);
    }
}
