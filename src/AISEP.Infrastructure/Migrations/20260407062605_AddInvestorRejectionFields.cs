using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestorRejectionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IDProofFileName",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvestmentProofFileName",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionRemarks",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresNewEvidence",
                table: "Investors",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IDProofFileName",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "InvestmentProofFileName",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "RejectionRemarks",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "RequiresNewEvidence",
                table: "Investors");
        }
    }
}
