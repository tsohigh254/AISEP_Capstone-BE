using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferencesMarketScopesAndSupportOffered : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredMarketScopes",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportOffered",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredMarketScopes",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "SupportOffered",
                table: "InvestorPreferences");

            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)2);
        }
    }
}
