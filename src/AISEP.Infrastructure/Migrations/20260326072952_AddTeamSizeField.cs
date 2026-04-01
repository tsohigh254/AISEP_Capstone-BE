using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamSizeField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TeamSize",
                table: "Startups",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TeamSize",
                table: "Startups");
        }
    }
}
