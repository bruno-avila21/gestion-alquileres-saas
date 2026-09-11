using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_visible_to_tenant",
                table: "documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Preserve current behaviour for documents that already existed: tenants could see every
            // document of their contract before this column existed, so keep those visible. New uploads
            // default to false (private) — the secure-by-default posture (audit A2).
            migrationBuilder.Sql("UPDATE documents SET is_visible_to_tenant = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_visible_to_tenant",
                table: "documents");
        }
    }
}
