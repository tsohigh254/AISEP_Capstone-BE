using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestorPreferencesNewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcceptingConnectionsStatus",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiScoreImportance",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvoidText",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PreferredAiScoreMax",
                table: "InvestorPreferences",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PreferredAiScoreMin",
                table: "InvestorPreferences",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredProductMaturity",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredStrengths",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredValidationLevel",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecentlyActiveBadge",
                table: "InvestorPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireVerifiedStartups",
                table: "InvestorPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireVisibleProfiles",
                table: "InvestorPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptingConnectionsStatus",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "AiScoreImportance",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "AvoidText",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredAiScoreMax",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredAiScoreMin",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredProductMaturity",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredStrengths",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredValidationLevel",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "RecentlyActiveBadge",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "RequireVerifiedStartups",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "RequireVisibleProfiles",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "InvestorPreferences");
        }
    }
}
