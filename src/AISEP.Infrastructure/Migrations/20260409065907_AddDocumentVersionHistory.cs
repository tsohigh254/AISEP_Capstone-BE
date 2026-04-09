using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentVersionHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentDocumentID",
                table: "Documents",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ParentDocumentID",
                table: "Documents",
                column: "ParentDocumentID");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Documents_ParentDocumentID",
                table: "Documents",
                column: "ParentDocumentID",
                principalTable: "Documents",
                principalColumn: "DocumentID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Documents_ParentDocumentID",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ParentDocumentID",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ParentDocumentID",
                table: "Documents");
        }
    }
}
