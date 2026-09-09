using System.Net.Http.Json;
using GestionAlquileres.API.Jobs;
using GestionAlquileres.Application.Common.Time;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GestionAlquileres.Tests.Phase7;

/// <summary>Cuenta los avisos de vencimiento enviados, por destinatario.</summary>
public class CountingEmailService : IEmailService
{
    public List<string> ExpiryNotifications { get; } = new();

    public Task SendRentAdjustmentNotificationAsync(
        string toEmail, string tenantName, string propertyAddress, decimal previousRent, decimal newRent,
        AdjustmentType adjustmentType, decimal adjustmentFactor, DateOnly effectiveDate, CancellationToken ct) =>
        Task.CompletedTask;

    public Task SendContractExpiryNotificationAsync(
        string toEmail, string tenantName, string propertyAddress, DateOnly expiryDate,
        int daysRemaining, CancellationToken ct)
    {
        ExpiryNotifications.Add(toEmail);
        return Task.CompletedTask;
    }

    public Task SendNewLeadNotificationAsync(
        string toEmail, string organizationName, string leadName, string? leadEmail, string? leadPhone,
        string message, string? propertyTitle, string? propertyAddress, Guid leadId, CancellationToken ct) =>
        Task.CompletedTask;
}

public class ExpiryJobApiFactory : Phase7ApiFactory
{
    public CountingEmailService Email { get; } = new();

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

public class ContractExpiryNotificationJobTests : IClassFixture<ExpiryJobApiFactory>
{
    private readonly ExpiryJobApiFactory _factory;
    public ContractExpiryNotificationJobTests(ExpiryJobApiFactory factory) => _factory = factory;

    private ContractExpiryNotificationJob CreateJob() =>
        new(_factory.Services, NullLogger<ContractExpiryNotificationJob>.Instance);

    /// <summary>Crea un contrato que vence dentro de la ventana de aviso (30 días).</summary>
    private static async Task CreateExpiringContractAsync(HttpClient client, string slug, int daysUntilExpiry)
    {
        var propResp = await client.PostAsJsonAsync("/api/v1/properties", new
        {
            address = "Sarmiento 500", city = "CABA", province = "CABA",
            propertyType = "Apartment", areaM2 = (decimal?)null, notes = (string?)null,
        });
        propResp.EnsureSuccessStatusCode();
        var propId = (await propResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var tenantResp = await client.PostAsJsonAsync("/api/v1/tenants", new
        {
            firstName = "Carla", lastName = "Gómez", dni = "30111222",
            email = $"carla@{slug}.com", phone = (string?)null,
        });
        tenantResp.EnsureSuccessStatusCode();
        var tenantId = (await tenantResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>())
            .GetProperty("id").GetGuid();

        var today = ArgentinaTime.Today;
        var contractResp = await client.PostAsJsonAsync("/api/v1/contracts", new
        {
            propertyId = propId,
            appTenantId = tenantId,
            startDate = today.AddYears(-2).ToString("yyyy-MM-dd"),
            endDate = today.AddDays(daysUntilExpiry).ToString("yyyy-MM-dd"),
            monthlyRent = 250_000m,
            currency = "ARS",
            adjustmentType = "Manual",
            adjustmentFrequency = "Quarterly",
            dayOfMonth = 1,
            depositAmount = (decimal?)null,
            notes = (string?)null,
        });
        contractResp.EnsureSuccessStatusCode();
    }

    // Regresión (auditoría 2026-07-31): el job corre a diario sobre una ventana de 30 días y no
    // tenía marcador de envío, así que le mandaba el MISMO aviso al mismo inquilino hasta 30 veces.
    [Fact]
    public async Task Job_RunTwice_SendsOnlyOneNotification()
    {
        const string slug = "expiry-dedupe";
        var client = await _factory.AuthedClientAsync(slug);
        await CreateExpiringContractAsync(client, slug, daysUntilExpiry: 10);

        var job = CreateJob();
        await job.ExecuteAsync();
        await job.ExecuteAsync();

        var toCarla = _factory.Email.ExpiryNotifications.Count(e => e == $"carla@{slug}.com");
        Assert.Equal(1, toCarla);
    }

    // Un contrato fuera de la ventana no debe generar ningún aviso.
    [Fact]
    public async Task Job_ContractOutsideWindow_SendsNothing()
    {
        const string slug = "expiry-far";
        var client = await _factory.AuthedClientAsync(slug);
        await CreateExpiringContractAsync(client, slug, daysUntilExpiry: 200);

        await CreateJob().ExecuteAsync();

        Assert.DoesNotContain($"carla@{slug}.com", _factory.Email.ExpiryNotifications);
    }

    // Un contrato que vence justo hoy sigue entrando en la ventana: es el borde que el uso de
    // DateTime.UtcNow en GetExpiringRawAsync desplazaba un día entre las 21:00 y las 24:00 AR.
    [Fact]
    public async Task Job_ContractExpiringToday_IsNotified()
    {
        const string slug = "expiry-today";
        var client = await _factory.AuthedClientAsync(slug);
        await CreateExpiringContractAsync(client, slug, daysUntilExpiry: 0);

        await CreateJob().ExecuteAsync();

        Assert.Contains($"carla@{slug}.com", _factory.Email.ExpiryNotifications);
    }
}
