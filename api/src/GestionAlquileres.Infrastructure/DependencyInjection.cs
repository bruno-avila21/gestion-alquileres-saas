using GestionAlquileres.Application.Common.Settings;
using GestionAlquileres.Domain.Interfaces.Repositories;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.ExternalServices;
using GestionAlquileres.Infrastructure.Persistence;
using GestionAlquileres.Infrastructure.Persistence.Repositories;
using GestionAlquileres.Infrastructure.Services;
using GestionAlquileres.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace GestionAlquileres.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection missing");

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString)
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<ICurrentTenant, CurrentTenantService>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IIndexRepository, IndexRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IAppTenantRepository, AppTenantRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IRentHistoryRepository, RentHistoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IStorageService, LocalFileStorageService>();
        services.AddSingleton<IDocumentTokenService, DocumentTokenService>();
        services.AddScoped<IEmailService, NullEmailService>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddScoped<IJwtService, JwtService>();

        // Index data now comes from the standalone indices-api (not BCRA/INDEC directly).
        // The base URL is configuration-driven; the API key is sent as X-Api-Key when present.
        var indicesBaseUrl = configuration["IndicesApi:BaseUrl"] ?? "http://localhost:5000";
        var indicesApiKey = configuration["IndicesApi:ApiKey"];
        services.AddHttpClient<IndicesApiClient>(client =>
        {
            client.BaseAddress = new Uri(indicesBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("User-Agent", "GestionAlquileres/1.0");
            if (!string.IsNullOrWhiteSpace(indicesApiKey))
                client.DefaultRequestHeaders.Add("X-Api-Key", indicesApiKey);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddStandardResilienceHandler(options =>
        {
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromSeconds(2);
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
        });

        // The SaaS index-sync handler depends on these two interfaces; both are now served by indices-api.
        services.AddScoped<IBcraApiClient>(sp => sp.GetRequiredService<IndicesApiClient>());
        services.AddScoped<IIndecApiClient>(sp => sp.GetRequiredService<IndicesApiClient>());
        services.AddScoped<IIndicesCalculator>(sp => sp.GetRequiredService<IndicesApiClient>());

        return services;
    }
}
