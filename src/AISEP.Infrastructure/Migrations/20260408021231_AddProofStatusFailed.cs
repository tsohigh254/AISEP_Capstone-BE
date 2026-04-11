using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProofStatusFailed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_ReviewedByUserUserID",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Users_CreatedByUserUserID",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Users_ResolvedByUserUserID",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_CreatedByUserUserID",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ResolvedByUserUserID",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ReviewedByUserUserID",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CreatedByUserUserID",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ResolvedByUserUserID",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserUserID",
                table: "Documents");

            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CreatedBy",
                table: "Incidents",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ResolvedBy",
                table: "Incidents",
                column: "ResolvedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ReviewedBy",
                table: "Documents",
                column: "ReviewedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_ReviewedBy",
                table: "Documents",
                column: "ReviewedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Users_CreatedBy",
                table: "Incidents",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Users_ResolvedBy",
                table: "Incidents",
                column: "ResolvedBy",
                principalTable: "Users",
                principalColumn: "UserID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_ReviewedBy",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Users_CreatedBy",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Users_ResolvedBy",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_CreatedBy",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_ResolvedBy",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ReviewedBy",
                table: "Documents");

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserUserID",
                table: "Incidents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolvedByUserUserID",
                table: "Incidents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByUserUserID",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)2);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CreatedByUserUserID",
                table: "Incidents",
                column: "CreatedByUserUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ResolvedByUserUserID",
                table: "Incidents",
                column: "ResolvedByUserUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ReviewedByUserUserID",
                table: "Documents",
                column: "ReviewedByUserUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Users_ReviewedByUserUserID",
                table: "Documents",
                column: "ReviewedByUserUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Users_CreatedByUserUserID",
                table: "Incidents",
                column: "CreatedByUserUserID",
                principalTable: "Users",
                principalColumn: "UserID");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Users_ResolvedByUserUserID",
                table: "Incidents",
                column: "ResolvedByUserUserID",
                principalTable: "Users",
                principalColumn: "UserID");
        }
    }
}
