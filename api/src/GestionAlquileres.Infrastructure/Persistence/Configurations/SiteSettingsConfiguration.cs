using GestionAlquileres.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionAlquileres.Infrastructure.Persistence.Configurations;

public class SiteSettingsConfiguration : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> builder)
    {
        builder.ToTable("site_settings");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");

        // Una sola configuración por organización: el índice único lo hace cumplir en la base
        // y no sólo en el handler, así que dos guardados simultáneos no pueden dejar dos filas.
        builder.HasIndex(s => s.OrganizationId).IsUnique();

        builder.Property(s => s.AccentColor).HasMaxLength(7);
        builder.Property(s => s.FontPairing).HasMaxLength(20);
        builder.Property(s => s.HeroTitle).HasMaxLength(160);
        builder.Property(s => s.HeroSubtitle).HasMaxLength(400);
        builder.Property(s => s.AboutText).HasMaxLength(2000);
        builder.Property(s => s.FooterTagline).HasMaxLength(200);
        builder.Property(s => s.UpdatedAt).HasDefaultValueSql("now()");
    }
}
