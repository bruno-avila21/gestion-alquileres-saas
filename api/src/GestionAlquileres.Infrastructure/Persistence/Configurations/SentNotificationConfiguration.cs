using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class SentNotificationConfiguration : IEntityTypeConfiguration<SentNotification>
{
    public void Configure(EntityTypeBuilder<SentNotification> builder)
    {
        builder.ToTable("sent_notifications");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(n => n.OrganizationId).IsRequired();
        builder.Property(n => n.ContractId).IsRequired();

        // Enum como string: reordenar NotificationKind no debe reinterpretar filas ya escritas.
        builder.Property(n => n.Kind).HasConversion<string>().HasMaxLength(40).IsRequired();

        builder.Property(n => n.DedupeKey).HasMaxLength(64).IsRequired();
        builder.Property(n => n.SentAt).HasDefaultValueSql("now()");

        // Guarda de idempotencia a nivel base: un aviso por contrato, tipo y evento lógico.
        // Es la red que sostiene al job si dos instancias corren a la vez o si un reintento
        // se solapa con la corrida programada.
        builder.HasIndex(n => new { n.ContractId, n.Kind, n.DedupeKey })
               .IsUnique()
               .HasDatabaseName("ix_sent_notifications_contract_kind_key_unique");

        // Toda tabla multi-tenant lleva un índice que arranca por organization_id.
        builder.HasIndex(n => new { n.OrganizationId, n.SentAt });

        builder.HasOne<Contract>()
            .WithMany()
            .HasForeignKey(n => n.ContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(n => n.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
