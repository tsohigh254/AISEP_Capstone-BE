using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveYearsOfExperienceToAdvisorExpertise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Advisors");

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "AdvisorExpertises",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "AdvisorExpertises");

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Advisors",
                type: "integer",
                nullable: true);
        }
    }
}
