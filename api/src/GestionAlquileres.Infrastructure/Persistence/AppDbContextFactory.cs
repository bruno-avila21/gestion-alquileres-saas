using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GestionAlquileres.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used only by the EF Core CLI (migrations add / database update). It builds
/// the DbContext without booting the web host, so secret validation and Hangfire setup don't run.
/// The connection string is a placeholder — migrations are generated from the model, not a live DB.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=design_time;Username=postgres;Password=postgres")
            .UseSnakeCaseNamingConvention()
            .Options;

        return new AppDbContext(options, new DesignTimeTenant());
    }

    private sealed class DesignTimeTenant : ICurrentTenant
    {
        public Guid OrganizationId => Guid.Empty;
    }
}
