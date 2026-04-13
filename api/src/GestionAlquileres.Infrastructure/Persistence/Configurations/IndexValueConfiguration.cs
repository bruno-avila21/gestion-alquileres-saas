using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class IndexValueConfiguration : IEntityTypeConfiguration<IndexValue>
{
    public void Configure(EntityTypeBuilder<IndexValue> builder)
    {
        builder.ToTable("index_values");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
               .HasDefaultValueSql("gen_random_uuid()");

        // Store enum as short integer (smallint). Matches IndexType: ICL=1, IPC=2.
        builder.Property(x => x.IndexType)
               .HasConversion<short>()
               .IsRequired();

        builder.Property(x => x.Period)
               .HasColumnType("date")
               .IsRequired();

        builder.Property(x => x.Value)
               .HasPrecision(18, 6)
               .IsRequired();

        builder.Property(x => x.VariationPct)
               .HasPrecision(10, 6);

        builder.Property(x => x.Source)
               .HasMaxLength(50)
               .IsRequired();

        builder.Property(x => x.FetchedAt)
               .HasDefaultValueSql("now()");

        // IDX-03 + RESEARCH Pitfall 3: prevent duplicate sync for same period.
        builder.HasIndex(x => new { x.IndexType, x.Period })
               .IsUnique()
               .HasDatabaseName("ix_index_values_type_period_unique");
    }
}
