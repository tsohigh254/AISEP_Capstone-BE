using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStartupProfileFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"ContactEmail\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"ContactPhone\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"Country\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"CurrentNeeds\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"IsVisible\" boolean NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"LinkedInURL\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"Location\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"MarketScope\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"MetricSummary\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"ProblemStatement\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"ProductStatus\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"SolutionSummary\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Startups\" ADD COLUMN IF NOT EXISTS \"SubIndustry\" text;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "CurrentNeeds",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "IsVisible",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "LinkedInURL",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "MarketScope",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "MetricSummary",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "ProblemStatement",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "ProductStatus",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "SolutionSummary",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "SubIndustry",
                table: "Startups");
        }
    }
}
