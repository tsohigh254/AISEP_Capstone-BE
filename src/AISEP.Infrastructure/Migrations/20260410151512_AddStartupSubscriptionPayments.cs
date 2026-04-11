using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartupSubscriptionPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StartupSubscriptionPayments",
                columns: table => new
                {
                    PaymentID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    TargetPlan = table.Column<short>(type: "smallint", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    TransactionCode = table.Column<int>(type: "integer", nullable: true),
                    PaymentStatus = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupSubscriptionPayments", x => x.PaymentID);
                    table.ForeignKey(
                        name: "FK_StartupSubscriptionPayments_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StartupSubscriptionPayments_StartupID",
                table: "StartupSubscriptionPayments",
                column: "StartupID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StartupSubscriptionPayments");
        }
    }
}
