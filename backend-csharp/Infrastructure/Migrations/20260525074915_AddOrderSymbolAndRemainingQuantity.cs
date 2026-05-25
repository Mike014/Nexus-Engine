using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusEngine.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderSymbolAndRemainingQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RemainingQuantity",
                table: "orders",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Symbol",
                table: "orders",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingQuantity",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "Symbol",
                table: "orders");
        }
    }
}
