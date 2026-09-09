using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class PropertyPhotoConfiguration : IEntityTypeConfiguration<PropertyPhoto>
{
    public void Configure(EntityTypeBuilder<PropertyPhoto> builder)
    {
        builder.ToTable("property_photos");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.OrganizationId).IsRequired();
        builder.Property(p => p.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(p => p.MimeType).IsRequired().HasMaxLength(100);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => new { p.PropertyId, p.SortOrder });

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Property>()
            .WithMany()
            .HasForeignKey(p => p.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
