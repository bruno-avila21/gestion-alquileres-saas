using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class AppTenantConfiguration : IEntityTypeConfiguration<AppTenant>
{
    public void Configure(EntityTypeBuilder<AppTenant> builder)
    {
        builder.ToTable("app_tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.OrganizationId).IsRequired();
        builder.Property(t => t.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(t => t.LastName).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Dni).IsRequired().HasMaxLength(15);
        builder.Property(t => t.Email).HasMaxLength(320);
        builder.Property(t => t.Phone).HasMaxLength(30);
        builder.Property(t => t.IsActive).HasDefaultValue(true);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(t => new { t.OrganizationId, t.Dni }).IsUnique();
        builder.HasIndex(t => t.OrganizationId);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
    }
}
