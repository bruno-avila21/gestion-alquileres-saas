using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("listings");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(l => l.OrganizationId).IsRequired();
        builder.Property(l => l.OperationType).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(l => l.Price).HasPrecision(18, 2);
        builder.Property(l => l.Currency).HasConversion<string>().IsRequired().HasMaxLength(3);
        builder.Property(l => l.Expenses).HasPrecision(18, 2);
        builder.Property(l => l.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(l => l.Title).IsRequired().HasMaxLength(200);
        builder.Property(l => l.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(l => l.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(l => l.OrganizationId);
        builder.HasIndex(l => new { l.OrganizationId, l.Status });
        builder.HasIndex(l => l.PropertyId);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(l => l.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.Property)
            .WithMany()
            .HasForeignKey(l => l.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
