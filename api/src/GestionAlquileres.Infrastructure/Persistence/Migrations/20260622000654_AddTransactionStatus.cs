using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionAlquileres.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "due_date",
                table: "transactions",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "paid_at",
                table: "transactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "status",
                table: "transactions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_transactions_status_due_date",
                table: "transactions",
                columns: new[] { "status", "due_date" });

            // Backfill: existing payments (type 1) and manual credits (type 3) are settled by nature,
            // so mark them Paid (status 1) and stamp paid_at. Charges keep the Pending default.
            migrationBuilder.Sql(
                "UPDATE transactions SET status = 1, paid_at = created_at WHERE type IN (1, 3);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_transactions_status_due_date",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "due_date",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "paid_at",
                table: "transactions");

            migrationBuilder.DropColumn(
                name: "status",
                table: "transactions");
        }
    }
}
