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
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""AcceptingConnectionsStatus"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""AiScoreImportance"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""AvoidText"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""PreferredAiScoreMax"" real;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""PreferredAiScoreMin"" real;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""PreferredProductMaturity"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""PreferredStrengths"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""PreferredValidationLevel"" text;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""RecentlyActiveBadge"" boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""RequireVerifiedStartups"" boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""RequireVisibleProfiles"" boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql(@"ALTER TABLE ""InvestorPreferences"" ADD COLUMN IF NOT EXISTS ""Tags"" text;");
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
