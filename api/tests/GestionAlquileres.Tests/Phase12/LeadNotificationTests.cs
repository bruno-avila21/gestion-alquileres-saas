using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionAlquileres.Application.Features.Listings.DTOs;
using GestionAlquileres.Application.Features.Properties.DTOs;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Tests.Phase7;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace GestionAlquileres.Tests.Phase12;

/// <summary>Cuenta los avisos de nueva consulta enviados, por destinatario/lead.</summary>
public class CountingLeadEmailService : IEmailService
{
    public List<(string ToEmail, string OrganizationName, string LeadName, string? PropertyTitle, Guid LeadId)> NewLeadNotifications { get; } = new();

    public Task SendRentAdjustmentNotificationAsync(
        string toEmail, string tenantName, string propertyAddress, decimal previousRent, decimal newRent,
        AdjustmentType adjustmentType, decimal adjustmentFactor, DateOnly effectiveDate, CancellationToken ct) =>
        Task.CompletedTask;

    public Task SendContractExpiryNotificationAsync(
        string toEmail, string tenantName, string propertyAddress, DateOnly expiryDate,
        int daysRemaining, CancellationToken ct) =>
        Task.CompletedTask;

    public Task SendNewLeadNotificationAsync(
        string toEmail, string organizationName, string leadName, string? leadEmail, string? leadPhone,
        string message, string? propertyTitle, string? propertyAddress, Guid leadId, CancellationToken ct)
    {
        NewLeadNotifications.Add((toEmail, organizationName, leadName, propertyTitle, leadId));
        return Task.CompletedTask;
    }
}

public class LeadNotificationApiFactory : Phase7ApiFactory
{
    public CountingLeadEmailService Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            var existing = services.Where(d => d.ServiceType == typeof(IEmailService)).ToList();
            foreach (var d in existing) services.Remove(d);
            services.AddSingleton<IEmailService>(Email);
        });
    }
}

/// <summary>Aviso por email al entrar una consulta pública (bloque A3, complemento del CRM de leads).</summary>
[Trait("Phase", "Phase12")]
public class LeadNotificationTests : IClassFixture<LeadNotificationApiFactory>
{
    private readonly LeadNotificationApiFactory _factory;
    public LeadNotificationTests(LeadNotificationApiFactory factory) => _factory = factory;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

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
    public async Task T1_Public_lead_with_listing_notifies_the_org_admin_once()
    {
        const string slug = "p12-notif-1";
        var c = await _factory.AuthedClientAsync(slug);
        var p = await CreatePropertyAsync(c);
        var listing = await CreatePublishedListingAsync(c, p.Id);

        var before = _factory.Email.NewLeadNotifications.Count;

        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync($"/api/v1/public/{slug}/leads", new
        {
            name = "Juana Pérez",
            email = "juana@example.com",
            phone = (string?)null,
            message = "Me interesa esta propiedad, ¿sigue disponible?",
            listingId = listing.Id,
            website = "",
        }, Json);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var sent = _factory.Email.NewLeadNotifications.Skip(before).ToList();
        var notification = Assert.Single(sent);
        Assert.Equal($"admin@{slug}.com", notification.ToEmail);
        Assert.Equal("Juana Pérez", notification.LeadName);
        Assert.Equal("Depto en Caballito", notification.PropertyTitle);
    }

    [Fact]
    public async Task T2_Public_lead_without_listing_notifies_the_org_admin_once()
    {
        const string slug = "p12-notif-2";
        var c = await _factory.AuthedClientAsync(slug);

        var before = _factory.Email.NewLeadNotifications.Count;

        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync($"/api/v1/public/{slug}/leads", new
        {
            name = "Consulta General",
            email = "general@example.com",
            message = "Quiero info general de alquileres",
            website = "",
        }, Json);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var sent = _factory.Email.NewLeadNotifications.Skip(before).ToList();
        var notification = Assert.Single(sent);
        Assert.Equal($"admin@{slug}.com", notification.ToEmail);
        Assert.Null(notification.PropertyTitle);
    }

    [Fact]
    public async Task T3_Honeypot_does_not_trigger_any_notification()
    {
        const string slug = "p12-notif-3";
        _ = await _factory.AuthedClientAsync(slug);

        var before = _factory.Email.NewLeadNotifications.Count;

        var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync($"/api/v1/public/{slug}/leads", new
        {
            name = "Bot",
            email = "bot@example.com",
            message = "Spam",
            website = "http://spam.example.com",
        }, Json);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        Assert.Equal(before, _factory.Email.NewLeadNotifications.Count);
    }
}
