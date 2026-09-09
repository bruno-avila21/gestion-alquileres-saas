using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(l => l.OrganizationId).IsRequired();

        builder.Property(l => l.Name).IsRequired().HasMaxLength(120);
        builder.Property(l => l.Email).HasMaxLength(200);
        builder.Property(l => l.Phone).HasMaxLength(40);
        builder.Property(l => l.Message).IsRequired().HasMaxLength(2000);
        builder.Property(l => l.LostReason).HasMaxLength(300);

        builder.Property(l => l.Source).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(l => l.Status).HasConversion<string>().IsRequired().HasMaxLength(20);

        builder.Property(l => l.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(l => l.UpdatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(l => new { l.OrganizationId, l.Status });
        builder.HasIndex(l => new { l.OrganizationId, l.CreatedAt });

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(l => l.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // A deleted listing/property must not take its leads down with it — the inquiry itself
        // (name, message, history) is worth keeping even if the ad is long gone.
        builder.HasOne(l => l.Listing)
            .WithMany()
            .HasForeignKey(l => l.ListingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(l => l.Property)
            .WithMany()
            .HasForeignKey(l => l.PropertyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(l => l.Notes)
            .WithOne(n => n.Lead)
            .HasForeignKey(n => n.LeadId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
