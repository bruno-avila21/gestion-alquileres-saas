using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToTable("contracts");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.OrganizationId).IsRequired();
        builder.Property(c => c.PropertyId).IsRequired();
        builder.Property(c => c.AppTenantId).IsRequired();
        builder.Property(c => c.MonthlyRent).HasPrecision(14, 2).IsRequired();
        builder.Property(c => c.DepositAmount).HasPrecision(14, 2);

        // Porcentaje de ajuste fijo. (6,3) admite hasta 999,999% — holgado para cualquier escalón
        // pactado, sin dejar de acotar una carga errónea a nivel base.
        builder.Property(c => c.AdjustmentPercent).HasPrecision(6, 3);
        builder.Property(c => c.DayOfMonth).IsRequired();
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(c => c.OrganizationId);
        builder.HasIndex(c => new { c.OrganizationId, c.Status });
        builder.HasIndex(c => c.AppTenantId);
        builder.HasIndex(c => c.PropertyId);

        // Optimistic concurrency on the contract (audit M7). The unique (ContractId, EffectiveDate)
        // index stops a duplicate RentHistory row, but it does NOT stop a lost update on MonthlyRent
        // when a manual adjustment and the scheduled job run concurrently with different effective
        // dates: both read the same previous rent and the last writer wins. Mapping PostgreSQL's system
        // column xmin as the concurrency token makes the second SaveChanges fail loudly (→ 409 via the
        // exception middleware) instead of silently overwriting. No real column is added.
        builder.UseXminAsConcurrencyToken();

        builder.HasOne(c => c.Property)
            .WithMany()
            .HasForeignKey(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.AppTenant)
            .WithMany()
            .HasForeignKey(c => c.AppTenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
