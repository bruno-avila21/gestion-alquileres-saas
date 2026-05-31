using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class RentHistoryConfiguration : IEntityTypeConfiguration<RentHistory>
{
    public void Configure(EntityTypeBuilder<RentHistory> builder)
    {
        builder.ToTable("rent_history");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(r => r.OrganizationId).IsRequired();
        builder.Property(r => r.ContractId).IsRequired();
        builder.Property(r => r.PreviousRent).HasPrecision(14, 2).IsRequired();
        builder.Property(r => r.NewRent).HasPrecision(14, 2).IsRequired();
        builder.Property(r => r.AdjustmentFactor).HasPrecision(10, 6).IsRequired();
        builder.Property(r => r.Notes).HasMaxLength(2000);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("now()");

        // Idempotency guard: at most one adjustment per contract per effective date.
        // Prevents double-application of a rent adjustment (job retry, manual + scheduled, concurrency).
        // The leftmost prefix (ContractId) also serves single-column ContractId lookups.
        builder.HasIndex(r => new { r.ContractId, r.EffectiveDate }).IsUnique();

        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(r => r.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(r => r.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<IndexValue>()
            .WithMany()
            .HasForeignKey(r => r.IndexValueId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
