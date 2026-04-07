using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdvisorWalletNWalletTransaction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualAmount",
                table: "StartupAdvisorMentorships",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "StartupAdvisorMentorships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "PaymentStatus",
                table: "StartupAdvisorMentorships",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<decimal>(
                name: "PlatformFeeAmount",
                table: "StartupAdvisorMentorships",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SessionAmount",
                table: "StartupAdvisorMentorships",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "TransactionCode",
                table: "StartupAdvisorMentorships",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WalletId",
                table: "Advisors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AdvisorWallets",
                columns: table => new
                {
                    WalletId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorId = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalEarned = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalWithdrawn = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorWallets", x => x.WalletId);
                });

            migrationBuilder.CreateTable(
                name: "WalletTransactions",
                columns: table => new
                {
                    TransactionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WalletId = table.Column<int>(type: "integer", nullable: false),
                    MentorshipID = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Type = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalletTransactions", x => x.TransactionID);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_AdvisorWallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "AdvisorWallets",
                        principalColumn: "WalletId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WalletTransactions_StartupAdvisorMentorships_MentorshipID",
                        column: x => x.MentorshipID,
                        principalTable: "StartupAdvisorMentorships",
                        principalColumn: "MentorshipID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Advisors_WalletId",
                table: "Advisors",
                column: "WalletId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_MentorshipID",
                table: "WalletTransactions",
                column: "MentorshipID");

            migrationBuilder.CreateIndex(
                name: "IX_WalletTransactions_WalletId",
                table: "WalletTransactions",
                column: "WalletId");

            migrationBuilder.AddForeignKey(
                name: "FK_Advisors_AdvisorWallets_WalletId",
                table: "Advisors",
                column: "WalletId",
                principalTable: "AdvisorWallets",
                principalColumn: "WalletId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Advisors_AdvisorWallets_WalletId",
                table: "Advisors");

            migrationBuilder.DropTable(
                name: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "AdvisorWallets");

            migrationBuilder.DropIndex(
                name: "IX_Advisors_WalletId",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "ActualAmount",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "PaymentStatus",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "PlatformFeeAmount",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "SessionAmount",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "TransactionCode",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "WalletId",
                table: "Advisors");
        }
    }
}
