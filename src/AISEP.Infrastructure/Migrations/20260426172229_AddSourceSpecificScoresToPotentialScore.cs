using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceSpecificScoresToPotentialScore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestorKycSubmissions_Users_ReviewedByUserUserID",
                table: "InvestorKycSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_InvestorKycSubmissions_InvestorID",
                table: "InvestorKycSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_InvestorKycSubmissions_ReviewedByUserUserID",
                table: "InvestorKycSubmissions");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserUserID",
                table: "InvestorKycSubmissions");

            migrationBuilder.AlterColumn<short>(
                name: "WorkflowStatus",
                table: "InvestorKycSubmissions",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<short>(
                name: "ResultLabel",
                table: "InvestorKycSubmissions",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.AlterColumn<short>(
                name: "Kind",
                table: "InvestorKycEvidenceFiles",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorKycSubmissions_InvestorID_IsActive",
                table: "InvestorKycSubmissions",
                columns: new[] { "InvestorID", "IsActive" },
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorKycSubmissions_InvestorID_Version",
                table: "InvestorKycSubmissions",
                columns: new[] { "InvestorID", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvestorKycSubmissions_ReviewedBy",
                table: "InvestorKycSubmissions",
                column: "ReviewedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_InvestorKycSubmissions_Users_ReviewedBy",
                table: "InvestorKycSubmissions",
                column: "ReviewedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvestorKycSubmissions_Users_ReviewedBy",
                table: "InvestorKycSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_InvestorKycSubmissions_InvestorID_IsActive",
                table: "InvestorKycSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_InvestorKycSubmissions_InvestorID_Version",
                table: "InvestorKycSubmissions");

            migrationBuilder.DropIndex(
                name: "IX_InvestorKycSubmissions_ReviewedBy",
                table: "InvestorKycSubmissions");

            migrationBuilder.AlterColumn<short>(
                name: "WorkflowStatus",
                table: "InvestorKycSubmissions",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)1);

            migrationBuilder.AlterColumn<short>(
                name: "ResultLabel",
                table: "InvestorKycSubmissions",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserUserID",
                table: "InvestorKycSubmissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "Kind",
                table: "InvestorKycEvidenceFiles",
                type: "smallint",
                nullable: false,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)2);

            migrationBuilder.CreateIndex(
                name: "IX_InvestorKycSubmissions_InvestorID",
                table: "InvestorKycSubmissions",
                column: "InvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorKycSubmissions_ReviewedByUserUserID",
                table: "InvestorKycSubmissions",
                column: "ReviewedByUserUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_InvestorKycSubmissions_Users_ReviewedByUserUserID",
                table: "InvestorKycSubmissions",
                column: "ReviewedByUserUserID",
                principalTable: "Users",
                principalColumn: "UserID");
        }
    }
}
