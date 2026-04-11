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
            // Use conditional SQL to handle environments where old shadow FK/index/columns
            // may or may not exist (production DB was set up without these shadow columns).

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Documents_Users_ReviewedByUserUserID') THEN
                        ALTER TABLE "Documents" DROP CONSTRAINT "FK_Documents_Users_ReviewedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Incidents_Users_CreatedByUserUserID') THEN
                        ALTER TABLE "Incidents" DROP CONSTRAINT "FK_Incidents_Users_CreatedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Incidents_Users_ResolvedByUserUserID') THEN
                        ALTER TABLE "Incidents" DROP CONSTRAINT "FK_Incidents_Users_ResolvedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Incidents_CreatedByUserUserID') THEN
                        DROP INDEX "IX_Incidents_CreatedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Incidents_ResolvedByUserUserID') THEN
                        DROP INDEX "IX_Incidents_ResolvedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Documents_ReviewedByUserUserID') THEN
                        DROP INDEX "IX_Documents_ReviewedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name = 'Incidents' AND column_name = 'CreatedByUserUserID') THEN
                        ALTER TABLE "Incidents" DROP COLUMN "CreatedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name = 'Incidents' AND column_name = 'ResolvedByUserUserID') THEN
                        ALTER TABLE "Incidents" DROP COLUMN "ResolvedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns
                               WHERE table_name = 'Documents' AND column_name = 'ReviewedByUserUserID') THEN
                        ALTER TABLE "Documents" DROP COLUMN "ReviewedByUserUserID";
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Incidents_CreatedBy') THEN
                        CREATE INDEX "IX_Incidents_CreatedBy" ON "Incidents" ("CreatedBy");
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Incidents_ResolvedBy') THEN
                        CREATE INDEX "IX_Incidents_ResolvedBy" ON "Incidents" ("ResolvedBy");
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'IX_Documents_ReviewedBy') THEN
                        CREATE INDEX "IX_Documents_ReviewedBy" ON "Documents" ("ReviewedBy");
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Documents_Users_ReviewedBy') THEN
                        ALTER TABLE "Documents" ADD CONSTRAINT "FK_Documents_Users_ReviewedBy"
                            FOREIGN KEY ("ReviewedBy") REFERENCES "Users" ("UserID") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Incidents_Users_CreatedBy') THEN
                        ALTER TABLE "Incidents" ADD CONSTRAINT "FK_Incidents_Users_CreatedBy"
                            FOREIGN KEY ("CreatedBy") REFERENCES "Users" ("UserID") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql("""
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Incidents_Users_ResolvedBy') THEN
                        ALTER TABLE "Incidents" ADD CONSTRAINT "FK_Incidents_Users_ResolvedBy"
                            FOREIGN KEY ("ResolvedBy") REFERENCES "Users" ("UserID") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);
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
