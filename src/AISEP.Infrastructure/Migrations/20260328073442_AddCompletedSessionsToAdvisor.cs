using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedSessionsToAdvisor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletedSessions",
                table: "Advisors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DomainTags",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExperiencesJson",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Expertise",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Advisors",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVerified",
                table: "Advisors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReviewCount",
                table: "Advisors",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Skills",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuitableFor",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupportedDurations",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Advisors",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedSessions",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "DomainTags",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "ExperiencesJson",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "Expertise",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "IsVerified",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "ReviewCount",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "SuitableFor",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "SupportedDurations",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Advisors");
        }
    }
}
