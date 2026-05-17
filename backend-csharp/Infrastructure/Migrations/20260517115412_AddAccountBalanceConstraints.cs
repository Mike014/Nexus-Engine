using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusEngine.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountBalanceConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "ck_accounts_balance_non_negative",
                table: "accounts",
                sql: "balance >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_accounts_reserved_balance_non_negative",
                table: "accounts",
                sql: "reserved_balance >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_accounts_balance_non_negative",
                table: "accounts");

            migrationBuilder.DropCheckConstraint(
                name: "ck_accounts_reserved_balance_non_negative",
                table: "accounts");
        }
    }
}
