using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260406150000_AddAdvisorOrganizationAndProofFile")]
    public partial class AddAdvisorOrganizationAndProofFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentOrganization",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BasicExpertiseProofFileURL",
                table: "Advisors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentOrganization",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "BasicExpertiseProofFileURL",
                table: "Advisors");
        }
    }
}
