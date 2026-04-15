using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBankInfoToAdvisorWallet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.AddColumn<string>(
                name: "AcceptingConnectionsStatus",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiScoreImportance",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvoidText",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PreferredAiScoreMax",
                table: "InvestorPreferences",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PreferredAiScoreMin",
                table: "InvestorPreferences",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredProductMaturity",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredStrengths",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredValidationLevel",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecentlyActiveBadge",
                table: "InvestorPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireVerifiedStartups",
                table: "InvestorPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequireVisibleProfiles",
                table: "InvestorPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "InvestorPreferences",
                type: "text",
                nullable: true);
            */

            migrationBuilder.AddColumn<string>(
                name: "BankAccountNumber",
                table: "AdvisorWallets",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankBin",
                table: "AdvisorWallets",
                type: "text",
                nullable: true);

            /*
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
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            /*
            migrationBuilder.DropTable(
                name: "AiEvaluationRuns");

            migrationBuilder.DropTable(
                name: "AiWebhookDeliveries");

            migrationBuilder.DropColumn(
                name: "AcceptingConnectionsStatus",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "AiScoreImportance",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "AvoidText",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredAiScoreMax",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredAiScoreMin",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredProductMaturity",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredStrengths",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "PreferredValidationLevel",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "RecentlyActiveBadge",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "RequireVerifiedStartups",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "RequireVisibleProfiles",
                table: "InvestorPreferences");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "InvestorPreferences");
            */

            migrationBuilder.DropColumn(
                name: "BankAccountNumber",
                table: "AdvisorWallets");

            migrationBuilder.DropColumn(
                name: "BankBin",
                table: "AdvisorWallets");
        }
    }
}
