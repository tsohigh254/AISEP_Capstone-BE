using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStartupAndItsRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Country",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "CoverImageURL",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "OneLiner",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "ProfileCompleteness",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "SubIndustry",
                table: "Startups");

            migrationBuilder.DropColumn(
                name: "TeamSize",
                table: "Startups");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CoverImageURL",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OneLiner",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileCompleteness",
                table: "Startups",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubIndustry",
                table: "Startups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamSize",
                table: "Startups",
                type: "integer",
                nullable: true);
        }
    }
}
