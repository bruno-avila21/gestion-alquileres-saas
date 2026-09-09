using GestionAlquileres.Domain.Entities;
using GestionAlquileres.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(t => t.OrganizationId).IsRequired();
        builder.Property(t => t.ContractId).IsRequired();
        builder.Property(t => t.Amount).HasPrecision(14, 2).IsRequired();
        builder.Property(t => t.Notes).HasMaxLength(2000);
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(t => t.Status).IsRequired().HasDefaultValue(TransactionStatus.Pending);
        builder.Property(t => t.ReceiptNumber).HasMaxLength(20);

        // Único por organización, sólo entre los no nulos: un recibo con número que cambia entre
        // descargas no sirve como comprobante, y dos transacciones nunca pueden compartir número.
        builder.HasIndex(t => new { t.OrganizationId, t.ReceiptNumber })
            .IsUnique()
            .HasFilter("receipt_number IS NOT NULL");

        builder.HasIndex(t => t.ContractId);
        builder.HasIndex(t => new { t.ContractId, t.Period });
        // Supports aging/morosidad queries over pending charges past their due date.
        builder.HasIndex(t => new { t.Status, t.DueDate });
        // The multi-tenant global filter adds `WHERE organization_id = @org` to every query, and the
        // org-wide listings order by Period; without this the highest-cardinality table sequential-scans
        // per tenant and sorts in memory (audit A4).
        builder.HasIndex(t => new { t.OrganizationId, t.Period });

        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(t => t.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(t => t.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
