using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiEvaluationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" DROP CONSTRAINT IF EXISTS ""FK_Documents_Users_ReviewedByUserUserID"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Incidents"" DROP CONSTRAINT IF EXISTS ""FK_Incidents_Users_CreatedByUserUserID"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Incidents"" DROP CONSTRAINT IF EXISTS ""FK_Incidents_Users_ResolvedByUserUserID"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Incidents_CreatedByUserUserID"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Incidents_ResolvedByUserUserID"";");
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_Documents_ReviewedByUserUserID"";");

            migrationBuilder.Sql(@"ALTER TABLE ""Incidents"" DROP COLUMN IF EXISTS ""CreatedByUserUserID"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Incidents"" DROP COLUMN IF EXISTS ""ResolvedByUserUserID"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" DROP COLUMN IF EXISTS ""ReviewedByUserUserID"";");

            migrationBuilder.AlterColumn<short>(
                name: "ProofStatus",
                table: "DocumentBlockchainProofs",
                type: "smallint",
                nullable: false,
                defaultValue: (short)2,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0);

            migrationBuilder.CreateTable(
                name: "AiEvaluationRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupId = table.Column<int>(type: "integer", nullable: false),
                    PythonRunId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    OverallScore = table.Column<double>(type: "double precision", nullable: true),
                    ReportJson = table.Column<string>(type: "text", nullable: true),
                    IsReportValid = table.Column<bool>(type: "boolean", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiEvaluationRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiEvaluationRuns_Startups_StartupId",
                        column: x => x.StartupId,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiWebhookDeliveries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeliveryId = table.Column<string>(type: "text", nullable: false),
                    EvaluationRunId = table.Column<int>(type: "integer", nullable: true),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Processed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessingNote = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiWebhookDeliveries", x => x.Id);
                });

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Incidents_CreatedBy"" ON ""Incidents"" (""CreatedBy"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Incidents_ResolvedBy"" ON ""Incidents"" (""ResolvedBy"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_Documents_ReviewedBy"" ON ""Documents"" (""ReviewedBy"");");

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationRuns_PythonRunId",
                table: "AiEvaluationRuns",
                column: "PythonRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AiEvaluationRuns_StartupId",
                table: "AiEvaluationRuns",
                column: "StartupId");

            migrationBuilder.CreateIndex(
                name: "IX_AiWebhookDeliveries_DeliveryId",
                table: "AiWebhookDeliveries",
                column: "DeliveryId",
                unique: true);

            migrationBuilder.Sql(@"ALTER TABLE ""Documents"" DROP CONSTRAINT IF EXISTS ""FK_Documents_Users_ReviewedBy""; ALTER TABLE ""Documents"" ADD CONSTRAINT ""FK_Documents_Users_ReviewedBy"" FOREIGN KEY (""ReviewedBy"") REFERENCES ""Users""(""UserID"") ON DELETE RESTRICT;");
            migrationBuilder.Sql(@"ALTER TABLE ""Incidents"" DROP CONSTRAINT IF EXISTS ""FK_Incidents_Users_CreatedBy""; ALTER TABLE ""Incidents"" ADD CONSTRAINT ""FK_Incidents_Users_CreatedBy"" FOREIGN KEY (""CreatedBy"") REFERENCES ""Users""(""UserID"") ON DELETE RESTRICT;");
            migrationBuilder.Sql(@"ALTER TABLE ""Incidents"" DROP CONSTRAINT IF EXISTS ""FK_Incidents_Users_ResolvedBy""; ALTER TABLE ""Incidents"" ADD CONSTRAINT ""FK_Incidents_Users_ResolvedBy"" FOREIGN KEY (""ResolvedBy"") REFERENCES ""Users""(""UserID"") ON DELETE RESTRICT;");
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

            migrationBuilder.DropTable(
                name: "AiEvaluationRuns");

            migrationBuilder.DropTable(
                name: "AiWebhookDeliveries");

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
