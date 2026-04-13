using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant currentTenant)
        : base(options)
    {
        _currentTenant = currentTenant;
    }

    public DbSet<IndexValue> Indexes => Set<IndexValue>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Organization is the tenant ROOT — no filter (filtering on its own PK is circular)
        // User and all future ITenantEntity implementations are filtered.
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.OrganizationId == _currentTenant.OrganizationId);

        // IndexValue is GLOBAL reference data (BCRA/INDEC) — no tenant filter.
        // Do NOT add HasQueryFilter for IndexValue.
    }
}
