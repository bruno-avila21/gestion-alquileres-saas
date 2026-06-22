using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(p => p.OrganizationId).IsRequired();
        builder.Property(p => p.Address).IsRequired().HasMaxLength(300);
        builder.Property(p => p.City).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Province).IsRequired().HasMaxLength(100);
        builder.Property(p => p.PropertyType).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(p => p.AreaM2).HasPrecision(10, 2);
        builder.Property(p => p.Notes).HasMaxLength(1000);
        builder.Property(p => p.CommissionPct).HasPrecision(5, 2);
        builder.Property(p => p.IsActive).HasDefaultValue(true);
        builder.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(p => p.OrganizationId);
        builder.HasIndex(p => p.OwnerId);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(p => p.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Owner link is optional; deleting an owner is blocked while properties reference it.
        builder.HasOne<Owner>()
            .WithMany()
            .HasForeignKey(p => p.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
