using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSiteSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "site_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accent_color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    font_pairing = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    hero_title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    hero_subtitle = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    about_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    footer_tagline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_site_settings", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_site_settings_organization_id",
                table: "site_settings",
                column: "organization_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "site_settings");
        }
    }
}
