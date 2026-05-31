using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(d => d.OrganizationId).IsRequired();
        builder.Property(d => d.ContractId).IsRequired();
        builder.Property(d => d.FileName).HasMaxLength(255).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(512).IsRequired();
        builder.Property(d => d.MimeType).HasMaxLength(127).IsRequired();
        builder.Property(d => d.SizeBytes).IsRequired();
        builder.Property(d => d.UploadedByUserId).IsRequired();
        builder.Property(d => d.CreatedAt).HasDefaultValueSql("now()");

        builder.HasIndex(d => d.OrganizationId);
        builder.HasIndex(d => d.ContractId);

        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(d => d.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(d => d.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
