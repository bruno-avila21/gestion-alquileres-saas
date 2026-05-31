using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure;
using GestionAlquileres.Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestionAlquileres.Tests.Infrastructure;

public class StorageSelectionTests
{
    private static IServiceProvider Build(Dictionary<string, string?> settings)
    {
        settings["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x";
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config); // the host registers this in production
        services.AddInfrastructure(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Defaults_to_local_storage()
    {
        using var sp = (ServiceProvider)Build(new Dictionary<string, string?>());
        using var scope = sp.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        Assert.IsType<LocalFileStorageService>(storage);
    }

    [Fact]
    public void Selects_s3_storage_when_provider_is_s3()
    {
        using var sp = (ServiceProvider)Build(new Dictionary<string, string?>
        {
            ["Storage:Provider"] = "S3",
            ["Storage:Bucket"] = "documents",
            ["Storage:ServiceUrl"] = "http://localhost:9000",
            ["Storage:AccessKey"] = "minioadmin",
            ["Storage:SecretKey"] = "minioadmin",
        });
        using var scope = sp.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        Assert.IsType<S3StorageService>(storage);
    }
}
