using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestorKYCFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "CurrentOrganization",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "CurrentRoleTitle",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "ProofFileURL",
                table: "Advisors");

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentOrganization",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentRoleTitle",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IDProofFileURL",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvestmentProofFileURL",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "InvestorType",
                table: "Investors",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmitterRole",
                table: "Investors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "CurrentOrganization",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "CurrentRoleTitle",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "IDProofFileURL",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "InvestmentProofFileURL",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "InvestorType",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "SubmitterRole",
                table: "Investors");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentOrganization",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentRoleTitle",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProofFileURL",
                table: "Advisors",
                type: "text",
                nullable: true);
        }
    }
}
