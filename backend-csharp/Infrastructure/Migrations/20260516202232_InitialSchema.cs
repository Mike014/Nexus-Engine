using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusEngine.Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    reserved_balance = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_event_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "domain_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    event_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    aggregate_version = table.Column<int>(type: "integer", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_domain_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                columns: table => new
                {
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    response_body = table.Column<string>(type: "jsonb", nullable: true),
                    response_status = table.Column<int>(type: "integer", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.idempotency_key);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    side = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    filled_quantity = table.Column<decimal>(type: "numeric(18,8)", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    last_event_version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.ForeignKey(
                        name: "FK_orders_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transactions", x => x.id);
                    table.ForeignKey(
                        name: "FK_transactions_accounts_account_id",
                        column: x => x.account_id,
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_domain_events_event_id",
                        column: x => x.event_id,
                        principalTable: "domain_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "idx_domain_events_occurred_at",
                table: "domain_events",
                column: "occurred_at");

            migrationBuilder.CreateIndex(
                name: "uq_aggregate_version",
                table: "domain_events",
                columns: new[] { "aggregate_id", "aggregate_version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_expires",
                table: "idempotency_keys",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_orders_account",
                table: "orders",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "idx_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "idx_transactions_account",
                table: "transactions",
                column: "account_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_event_id",
                table: "transactions",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "IX_transactions_order_id",
                table: "transactions",
                column: "order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys");

            migrationBuilder.DropTable(
                name: "transactions");

            migrationBuilder.DropTable(
                name: "domain_events");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "accounts");
        }
    }
}
