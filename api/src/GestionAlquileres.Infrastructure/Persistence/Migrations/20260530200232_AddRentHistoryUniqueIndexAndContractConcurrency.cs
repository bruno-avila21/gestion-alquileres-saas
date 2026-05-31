using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRentHistoryUniqueIndexAndContractConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rent_history_contract_id",
                table: "rent_history");

            migrationBuilder.DropIndex(
                name: "ix_rent_history_contract_id_effective_date",
                table: "rent_history");

            migrationBuilder.CreateIndex(
                name: "ix_rent_history_contract_id_effective_date",
                table: "rent_history",
                columns: new[] { "contract_id", "effective_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rent_history_contract_id_effective_date",
                table: "rent_history");

            migrationBuilder.CreateIndex(
                name: "ix_rent_history_contract_id",
                table: "rent_history",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_rent_history_contract_id_effective_date",
                table: "rent_history",
                columns: new[] { "contract_id", "effective_date" });
        }
    }
}
