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
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<AppTenant> AppTenants => Set<AppTenant>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<RentHistory> RentHistory => Set<RentHistory>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Organization is the tenant ROOT — no filter (filtering on its own PK is circular)
        // User and all future ITenantEntity implementations are filtered.
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => u.OrganizationId == _currentTenant.OrganizationId);

        modelBuilder.Entity<Owner>()
            .HasQueryFilter(o => o.OrganizationId == _currentTenant.OrganizationId);

        modelBuilder.Entity<Property>()
            .HasQueryFilter(p => p.OrganizationId == _currentTenant.OrganizationId);

        modelBuilder.Entity<AppTenant>()
            .HasQueryFilter(t => t.OrganizationId == _currentTenant.OrganizationId);

        modelBuilder.Entity<Contract>()
            .HasQueryFilter(c => c.OrganizationId == _currentTenant.OrganizationId);

        modelBuilder.Entity<RentHistory>()
            .HasQueryFilter(r => r.OrganizationId == _currentTenant.OrganizationId);

        modelBuilder.Entity<Transaction>()
            .HasQueryFilter(t => t.OrganizationId == _currentTenant.OrganizationId);

        modelBuilder.Entity<Document>()
            .HasQueryFilter(d => d.OrganizationId == _currentTenant.OrganizationId);

        // IndexValue is GLOBAL reference data (BCRA/INDEC) — no tenant filter.
        // Do NOT add HasQueryFilter for IndexValue.

        // RefreshToken has NO tenant filter on purpose: the /refresh endpoint runs without a tenant
        // in scope. Lookup is by globally-unique token hash; the owning org is carried on the row.
    }
}
