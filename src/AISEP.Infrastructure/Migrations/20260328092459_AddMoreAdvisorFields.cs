using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreAdvisorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
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
                name: "Company",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "Currency",
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
