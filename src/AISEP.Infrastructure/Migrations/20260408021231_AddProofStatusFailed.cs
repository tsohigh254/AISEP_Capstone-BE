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
            // Use raw SQL with IF EXISTS to handle out-of-sync DB
            // (production DB was partially created manually, so these
            // constraints/columns may not exist)

            migrationBuilder.Sql(@"
                ALTER TABLE ""Documents"" DROP CONSTRAINT IF EXISTS ""FK_Documents_Users_ReviewedByUserUserID"";
                ALTER TABLE ""Incidents"" DROP CONSTRAINT IF EXISTS ""FK_Incidents_Users_CreatedByUserUserID"";
                ALTER TABLE ""Incidents"" DROP CONSTRAINT IF EXISTS ""FK_Incidents_Users_ResolvedByUserUserID"";

                DROP INDEX IF EXISTS ""IX_Incidents_CreatedByUserUserID"";
                DROP INDEX IF EXISTS ""IX_Incidents_ResolvedByUserUserID"";
                DROP INDEX IF EXISTS ""IX_Documents_ReviewedByUserUserID"";

                ALTER TABLE ""Incidents"" DROP COLUMN IF EXISTS ""CreatedByUserUserID"";
                ALTER TABLE ""Incidents"" DROP COLUMN IF EXISTS ""ResolvedByUserUserID"";
                ALTER TABLE ""Documents"" DROP COLUMN IF EXISTS ""ReviewedByUserUserID"";
            ");

            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0);

            // Create indexes and FKs only if they don't already exist
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Incidents_CreatedBy"" ON ""Incidents"" (""CreatedBy"");
                CREATE INDEX IF NOT EXISTS ""IX_Incidents_ResolvedBy"" ON ""Incidents"" (""ResolvedBy"");
                CREATE INDEX IF NOT EXISTS ""IX_Documents_ReviewedBy"" ON ""Documents"" (""ReviewedBy"");
            ");

            migrationBuilder.Sql(@"
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Documents_Users_ReviewedBy') THEN
                        ALTER TABLE ""Documents"" ADD CONSTRAINT ""FK_Documents_Users_ReviewedBy""
                            FOREIGN KEY (""ReviewedBy"") REFERENCES ""Users""(""UserID"") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Incidents_Users_CreatedBy') THEN
                        ALTER TABLE ""Incidents"" ADD CONSTRAINT ""FK_Incidents_Users_CreatedBy""
                            FOREIGN KEY (""CreatedBy"") REFERENCES ""Users""(""UserID"") ON DELETE RESTRICT;
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Incidents_Users_ResolvedBy') THEN
                        ALTER TABLE ""Incidents"" ADD CONSTRAINT ""FK_Incidents_Users_ResolvedBy""
                            FOREIGN KEY (""ResolvedBy"") REFERENCES ""Users""(""UserID"") ON DELETE RESTRICT;
                    END IF;
                END $$;
            ");
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
