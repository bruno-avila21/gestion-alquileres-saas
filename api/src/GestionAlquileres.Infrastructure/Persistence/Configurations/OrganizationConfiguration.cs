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
    }
}
