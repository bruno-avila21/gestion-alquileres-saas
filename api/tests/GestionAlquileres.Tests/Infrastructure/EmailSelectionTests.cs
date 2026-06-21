using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure;
using GestionAlquileres.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GestionAlquileres.Tests.Infrastructure;

/// <summary>Audit M-1: SMTP email is used only when configured; otherwise the no-op logger.</summary>
public class EmailSelectionTests
{
    private static IServiceProvider Build(Dictionary<string, string?> settings)
    {
        settings["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=x";
        var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddInfrastructure(config);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Defaults_to_null_email_service()
    {
        using var sp = (ServiceProvider)Build(new Dictionary<string, string?>());
        using var scope = sp.CreateScope();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        Assert.IsType<NullEmailService>(email);
    }

    [Fact]
    public void Selects_smtp_email_service_when_provider_is_smtp()
    {
        using var sp = (ServiceProvider)Build(new Dictionary<string, string?>
        {
            ["Email:Provider"] = "Smtp",
            ["Email:Host"] = "smtp.example.com",
            ["Email:Username"] = "user",
            ["Email:Password"] = "pass",
        });
        using var scope = sp.CreateScope();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        Assert.IsType<SmtpEmailService>(email);
    }
}
