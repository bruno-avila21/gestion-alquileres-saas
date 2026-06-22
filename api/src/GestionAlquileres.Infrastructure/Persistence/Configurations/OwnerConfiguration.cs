using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("owners");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(o => o.OrganizationId).IsRequired();
        builder.Property(o => o.Name).IsRequired().HasMaxLength(200);
        builder.Property(o => o.TaxId).HasMaxLength(20);
        builder.Property(o => o.Email).HasMaxLength(256);
        builder.Property(o => o.Phone).HasMaxLength(40);
        builder.Property(o => o.Cbu).HasMaxLength(40);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.IsActive).HasDefaultValue(true);
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("now()");
        builder.HasIndex(o => o.OrganizationId);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(o => o.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
