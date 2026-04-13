using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using GestionAlquileres.Domain.Interfaces.Services;
using GestionAlquileres.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GestionAlquileres.Tests;

public class TenantIsolationTests
{
    private sealed class StubCurrentTenant : ICurrentTenant
    {
        public Guid OrganizationId { get; set; }
    }

    private static AppDbContext NewContext(StubCurrentTenant tenant, string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options, tenant);
    }

    [Fact]
    public async Task User_query_returns_only_current_tenant_rows()
    {
        var dbName = Guid.NewGuid().ToString();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        // Seed with global-filter bypass (via IgnoreQueryFilters on Add is N/A;
        // we insert from an unscoped tenant context using Guid.Empty that still
        // hits the filter but Add() is not filtered).
        var seedTenant = new StubCurrentTenant { OrganizationId = Guid.Empty };
        using (var seed = NewContext(seedTenant, dbName))
        {
            seed.Users.Add(new User { OrganizationId = orgA, Email = "a@a.com", FirstName="A", LastName="A", PasswordHash="x", Role = UserRole.Admin });
            seed.Users.Add(new User { OrganizationId = orgB, Email = "b@b.com", FirstName="B", LastName="B", PasswordHash="x", Role = UserRole.Admin });
            await seed.SaveChangesAsync();
        }

        // Query as Tenant A — must see only orgA user
        var tenantA = new StubCurrentTenant { OrganizationId = orgA };
        using var ctxA = NewContext(tenantA, dbName);
        var visibleToA = await ctxA.Users.ToListAsync();

        Assert.Single(visibleToA);
        Assert.Equal(orgA, visibleToA[0].OrganizationId);
        Assert.DoesNotContain(visibleToA, u => u.OrganizationId == orgB);
    }

    [Fact]
    public async Task User_query_returns_empty_when_tenant_is_empty()
    {
        var dbName = Guid.NewGuid().ToString();
        var orgA = Guid.NewGuid();

        var seedTenant = new StubCurrentTenant { OrganizationId = Guid.Empty };
        using (var seed = NewContext(seedTenant, dbName))
        {
            seed.Users.Add(new User { OrganizationId = orgA, Email = "a@a.com", FirstName="A", LastName="A", PasswordHash="x", Role = UserRole.Admin });
            await seed.SaveChangesAsync();
        }

        // Unauthenticated (Guid.Empty) -> filter matches nothing
        var emptyTenant = new StubCurrentTenant { OrganizationId = Guid.Empty };
        using var ctx = NewContext(emptyTenant, dbName);
        var visible = await ctx.Users.ToListAsync();

        Assert.Empty(visible);
    }

    [Fact]
    public void Organization_entity_has_no_query_filter()
    {
        var tenant = new StubCurrentTenant { OrganizationId = Guid.NewGuid() };
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var ctx = new AppDbContext(options, tenant);

        var orgEntityType = ctx.Model.FindEntityType(typeof(Organization));
        Assert.NotNull(orgEntityType);
        // EF Core 8: GetQueryFilter() returns null when none configured
        Assert.Null(orgEntityType!.GetQueryFilter());

        var userEntityType = ctx.Model.FindEntityType(typeof(User));
        Assert.NotNull(userEntityType!.GetQueryFilter());
    }
}
