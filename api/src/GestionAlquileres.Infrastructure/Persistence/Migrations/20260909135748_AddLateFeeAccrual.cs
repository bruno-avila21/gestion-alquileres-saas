using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLateFeeAccrual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "accrued_through_date",
                table: "transactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "related_transaction_id",
                table: "transactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "late_fee_daily_rate",
                table: "contracts",
                type: "numeric(6,4)",
                precision: 6,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "late_fee_grace_days",
                table: "contracts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_related_transaction_id",
                table: "transactions",
                column: "related_transaction_id",
                unique: true,
                filter: "related_transaction_id IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_transactions_transactions_related_transaction_id",
                table: "transactions",
                column: "related_transaction_id",
                principalTable: "transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_transactions_transactions_related_transaction_id",
                table: "transactions");

            migrationBuilder.DropIndex(
                name: "ix_transactions_related_transaction_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "accrued_through_date",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "related_transaction_id",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "late_fee_daily_rate",
                table: "contracts");

            migrationBuilder.DropColumn(
                name: "late_fee_grace_days",
                table: "contracts");
        }
    }
}
