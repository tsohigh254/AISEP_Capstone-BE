using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "Documents");

            migrationBuilder.RenameColumn(
                name: "FileFormat",
                table: "Documents",
                newName: "Title");

            // Convert DocumentType from string to smallint
            migrationBuilder.Sql("""
                ALTER TABLE "Documents"
                ALTER COLUMN "DocumentType" TYPE smallint
                USING CASE "DocumentType"
                    WHEN 'Pitch_Deck' THEN 0
                    WHEN 'Bussiness_Plan' THEN 1 
                    ELSE 0
                END;
            """);

            // Convert AnalysisStatus from string to smallint
            migrationBuilder.Sql("""
                ALTER TABLE "Documents"
                ALTER COLUMN "AnalysisStatus" TYPE smallint
                USING CASE "AnalysisStatus"
                    WHEN 'NOTANALYZE' THEN 0
                    WHEN 'COMPLETED' THEN 1 
                    WHEN 'FAILED' THEN 2
                    ELSE 0
                END;
            """);

            // Set AnalysisStatus default value after conversion
            migrationBuilder.Sql("""
                ALTER TABLE "Documents"
                ALTER COLUMN "AnalysisStatus" SET DEFAULT 0;
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Documents",
                newName: "FileFormat");

            // Reverse DocumentType conversion
            migrationBuilder.Sql("""
                ALTER TABLE "Documents"
                ALTER COLUMN "DocumentType" TYPE text
                USING CASE "DocumentType"
                    WHEN 0 THEN 'Pitch_Deck'
                    WHEN 1 THEN 'Bussiness_Plan'
                    ELSE 'Pitch_Deck'
                END;
            """);

            // Reverse AnalysisStatus conversion
            migrationBuilder.Sql("""
                ALTER TABLE "Documents"
                ALTER COLUMN "AnalysisStatus" TYPE text
                USING CASE "AnalysisStatus"
                    WHEN 0 THEN 'NOTANALYZE'
                    WHEN 1 THEN 'COMPLETED'
                    WHEN 2 THEN 'FAILED'
                    ELSE 'NOTANALYZE'
                END;
            """);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "FileSize",
                table: "Documents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
