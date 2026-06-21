using GestionAlquileres.API.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace GestionAlquileres.Tests.Infrastructure;

/// <summary>Audit A-3: outside Development, Storage:Provider must be S3 (local FS is a SPOF).</summary>
public class StorageProviderValidationTests
{
    private sealed class StubEnv : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static IConfiguration ConfigWith(string? storageProvider)
    {
        // Valid secrets/connection strings so the ONLY potential problem is storage.
        var values = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = new string('k', 40),
            ["DocumentToken:Secret"] = new string('s', 40),
            ["ConnectionStrings:DefaultConnection"] = "Host=db;Database=app;Username=app;Password=secure",
            ["ConnectionStrings:HangfireConnection"] = "Host=db;Database=hf;Username=app;Password=secure",
        };
        if (storageProvider is not null) values["Storage:Provider"] = storageProvider;
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    [Fact]
    public void Production_with_local_storage_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SecuritySettingsValidator.Validate(ConfigWith("Local"), new StubEnv()));
        Assert.Contains("Storage:Provider", ex.Message);
    }

    [Fact]
    public void Production_with_missing_storage_provider_throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SecuritySettingsValidator.Validate(ConfigWith(null), new StubEnv()));
        Assert.Contains("Storage:Provider", ex.Message);
    }

    [Fact]
    public void Production_with_s3_storage_passes()
    {
        // Should not throw — S3 is the required production provider.
        SecuritySettingsValidator.Validate(ConfigWith("S3"), new StubEnv());
    }

    [Fact]
    public void Development_with_local_storage_is_allowed()
    {
        var env = new StubEnv { EnvironmentName = Environments.Development };
        SecuritySettingsValidator.Validate(ConfigWith("Local"), env);
    }
}
