using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Slug).IsRequired().HasMaxLength(100);
        builder.HasIndex(o => o.Slug).IsUnique();
        builder.Property(o => o.Plan).IsRequired().HasMaxLength(20).HasDefaultValue("free");
        builder.Property(o => o.IsActive).HasDefaultValue(true);
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("now()");

        // Marca de la inmobiliaria (bloque PDF recibos/liquidaciones).
        builder.Property(o => o.LegalName).HasMaxLength(200);
        builder.Property(o => o.TaxId).HasMaxLength(20);
        builder.Property(o => o.Address).HasMaxLength(300);
        builder.Property(o => o.Phone).HasMaxLength(50);
        builder.Property(o => o.Email).HasMaxLength(200);
        builder.Property(o => o.LogoStorageKey).HasMaxLength(200);
        builder.Property(o => o.BrandColor).HasMaxLength(7);
        builder.Property(o => o.ReceiptSequence).HasDefaultValue(0L);
    }
}
