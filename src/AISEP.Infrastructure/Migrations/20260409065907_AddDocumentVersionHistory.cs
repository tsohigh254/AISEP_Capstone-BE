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
            migrationBuilder.Sql(@"
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""ParentDocumentID"" integer;

                CREATE INDEX IF NOT EXISTS ""IX_Documents_ParentDocumentID""
                    ON ""Documents"" (""ParentDocumentID"");

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Documents_Documents_ParentDocumentID') THEN
                        ALTER TABLE ""Documents"" ADD CONSTRAINT ""FK_Documents_Documents_ParentDocumentID""
                            FOREIGN KEY (""ParentDocumentID"") REFERENCES ""Documents""(""DocumentID"") ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");
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
