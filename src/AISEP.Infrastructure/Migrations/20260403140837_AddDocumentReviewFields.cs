using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use IF NOT EXISTS to handle out-of-sync DB
            // (production DB was partially created manually)
            migrationBuilder.Sql(@"
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""ReviewNotes"" text;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""ReviewStatus"" smallint NOT NULL DEFAULT 0;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""ReviewedAt"" timestamp with time zone;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""ReviewedBy"" integer;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""ReviewedByUserUserID"" integer;

                CREATE INDEX IF NOT EXISTS ""IX_Documents_ReviewedByUserUserID""
                    ON ""Documents"" (""ReviewedByUserUserID"");

                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Documents_Users_ReviewedByUserUserID') THEN
                        ALTER TABLE ""Documents"" ADD CONSTRAINT ""FK_Documents_Users_ReviewedByUserUserID""
                            FOREIGN KEY (""ReviewedByUserUserID"") REFERENCES ""Users""(""UserID"");
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Users_ReviewedByUserUserID",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ReviewedByUserUserID",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReviewNotes",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReviewStatus",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReviewedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReviewedByUserUserID",
                table: "Documents");
        }
    }
}
