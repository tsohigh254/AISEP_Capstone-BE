using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartupWalletAndRefundFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Database already contains StartupWallets and StartupWalletId column.
            // Marking migration as applied without changes.
        }


        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WalletTransactions_StartupWallets_StartupWalletId",
                table: "WalletTransactions");

            migrationBuilder.DropTable(
                name: "StartupWallets");

            migrationBuilder.DropIndex(
                name: "IX_WalletTransactions_StartupWalletId",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "StartupWalletId",
                table: "WalletTransactions");

            migrationBuilder.AlterColumn<int>(
                name: "WalletId",
                table: "WalletTransactions",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
