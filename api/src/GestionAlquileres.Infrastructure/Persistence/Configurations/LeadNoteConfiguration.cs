using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class LeadNoteConfiguration : IEntityTypeConfiguration<LeadNote>
{
    public void Configure(EntityTypeBuilder<LeadNote> builder)
    {
        builder.ToTable("lead_notes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(n => n.OrganizationId).IsRequired();

        builder.Property(n => n.Text).IsRequired().HasMaxLength(2000);
        builder.Property(n => n.CreatedByName).IsRequired().HasMaxLength(200);
        builder.Property(n => n.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(n => n.LeadId);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(n => n.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
