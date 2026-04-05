using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCountryToStartup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentNeeds",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetricSummary",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PitchDeckUrl",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductStatus",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubIndustry",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TeamSize",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleMeetLink",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MsTeamsLink",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Advisors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "CurrentNeeds",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "MetricSummary",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "PitchDeckUrl",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "ProductStatus",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "SubIndustry",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "TeamSize",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "GoogleMeetLink",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "MsTeamsLink",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Advisors");
        }
    }
}
