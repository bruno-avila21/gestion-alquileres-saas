using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddListingsAndPropertyPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "age_years",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "bathrooms",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "bedrooms",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "properties",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "covered_area_m2",
                table: "properties",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "properties",
                type: "character varying(5000)",
                maxLength: 5000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "features",
                table: "properties",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "garages",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "latitude",
                table: "properties",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "longitude",
                table: "properties",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "neighborhood",
                table: "properties",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "rooms",
                table: "properties",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "suitable_for_credit",
                table: "properties",
                type: "boolean",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "listings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    expenses = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_listings", x => x.id);
                    table.ForeignKey(
                        name: "fk_listings_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_listings_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "property_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_cover = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_property_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_property_photos_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_property_photos_properties_property_id",
                        column: x => x.property_id,
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_properties_organization_id_code",
                table: "properties",
                columns: new[] { "organization_id", "code" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_organization_id",
                table: "listings",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_listings_organization_id_status",
                table: "listings",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_listings_property_id",
                table: "listings",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_property_photos_organization_id",
                table: "property_photos",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_property_photos_property_id_sort_order",
                table: "property_photos",
                columns: new[] { "property_id", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "listings");

            migrationBuilder.DropTable(
                name: "property_photos");

            migrationBuilder.DropIndex(
                name: "ix_properties_organization_id_code",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "age_years",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "bathrooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "bedrooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "code",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "covered_area_m2",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "description",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "features",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "garages",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "latitude",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "longitude",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "neighborhood",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "rooms",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "suitable_for_credit",
                table: "properties");
        }
    }
}
