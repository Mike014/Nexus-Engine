using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusEngine.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderColumnNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Symbol",
                table: "orders",
                newName: "symbol");

            migrationBuilder.RenameColumn(
                name: "RemainingQuantity",
                table: "orders",
                newName: "remaining_quantity");

            migrationBuilder.AlterColumn<string>(
                name: "symbol",
                table: "orders",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<decimal>(
                name: "remaining_quantity",
                table: "orders",
                type: "numeric(18,8)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "symbol",
                table: "orders",
                newName: "Symbol");

            migrationBuilder.RenameColumn(
                name: "remaining_quantity",
                table: "orders",
                newName: "RemainingQuantity");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "orders",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<decimal>(
                name: "RemainingQuantity",
                table: "orders",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,8)");
        }
    }
}
