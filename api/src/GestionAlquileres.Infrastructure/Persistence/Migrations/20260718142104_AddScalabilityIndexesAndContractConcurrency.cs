using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScalabilityIndexesAndContractConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transactions_organization_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_rent_history_organization_id",
                table: "rent_history");

            // NOTE: xmin is a PostgreSQL system column that already exists on every table. The model
            // maps it as the optimistic-concurrency token (Contract.UseXminAsConcurrencyToken, audit
            // M7), but there is no DDL to run — EF's generated AddColumn was removed because creating a
            // real "xmin" column errors with "conflicts with a system column name".

            migrationBuilder.CreateIndex(
                name: "ix_transactions_organization_id_period",
                table: "transactions",
                columns: new[] { "organization_id", "period" });

            migrationBuilder.CreateIndex(
                name: "ix_rent_history_organization_id_effective_date",
                table: "rent_history",
                columns: new[] { "organization_id", "effective_date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transactions_organization_id_period",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_rent_history_organization_id_effective_date",
                table: "rent_history");

            // xmin is a system column — nothing to drop (see the note in Up).

            migrationBuilder.CreateIndex(
                name: "ix_transactions_organization_id",
                table: "transactions",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_rent_history_organization_id",
                table: "rent_history",
                column: "organization_id");
        }
    }
}
