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
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IAppTenantRepository, AppTenantRepository>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<IRentHistoryRepository, RentHistoryRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ISentNotificationRepository, SentNotificationRepository>();
        services.AddSingleton<IDocumentTokenService, DocumentTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Email: real SMTP relay when Email:Provider = "Smtp" (SES/Resend/etc.), else a no-op logger.
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        if (string.Equals(configuration["Email:Provider"], "Smtp", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IEmailService, SmtpEmailService>();
        else
            services.AddScoped<IEmailService, NullEmailService>();

        // Document storage: S3-compatible object store (AWS S3 / MinIO) when configured, else local FS.
        services.Configure<StorageSettings>(configuration.GetSection(StorageSettings.SectionName));
        AddStorage(services, configuration);

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

    private static void AddStorage(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Storage:Provider"] ?? "Local";

        if (!provider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IStorageService, LocalFileStorageService>();
            return;
        }

        services.AddSingleton<Amazon.S3.IAmazonS3>(_ =>
        {
            var config = new Amazon.S3.AmazonS3Config
            {
                ForcePathStyle = bool.TryParse(configuration["Storage:ForcePathStyle"], out var fps) ? fps : true,
            };

            var serviceUrl = configuration["Storage:ServiceUrl"]; // set for MinIO; null for AWS
            if (!string.IsNullOrWhiteSpace(serviceUrl))
                config.ServiceURL = serviceUrl;
            else
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(configuration["Storage:Region"] ?? "us-east-1");

            var accessKey = configuration["Storage:AccessKey"];
            var secretKey = configuration["Storage:SecretKey"];
            return string.IsNullOrWhiteSpace(accessKey)
                ? new Amazon.S3.AmazonS3Client(config) // fall back to the default AWS credential chain
                : new Amazon.S3.AmazonS3Client(accessKey, secretKey, config);
        });
        services.AddScoped<IStorageService, S3StorageService>();
    }
}
