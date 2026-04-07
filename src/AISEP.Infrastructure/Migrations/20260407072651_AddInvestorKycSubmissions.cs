using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInvestorKycSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessCode",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "CurrentOrganization",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "CurrentRoleTitle",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "IDProofFileName",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "IDProofFileURL",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "InvestmentProofFileName",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "InvestmentProofFileURL",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "InvestorType",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "RejectionRemarks",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "Remarks",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "RequiresNewEvidence",
                table: "Investors");

            migrationBuilder.DropColumn(
                name: "SubmitterRole",
                table: "Investors");

            migrationBuilder.CreateTable(
                name: "InvestorKycSubmissions",
                columns: table => new
                {
                    SubmissionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InvestorID = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    WorkflowStatus = table.Column<short>(type: "smallint", nullable: false),
                    ResultLabel = table.Column<short>(type: "smallint", nullable: false),
                    InvestorCategory = table.Column<string>(type: "text", nullable: true),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    ContactEmail = table.Column<string>(type: "text", nullable: true),
                    OrganizationName = table.Column<string>(type: "text", nullable: true),
                    CurrentRoleTitle = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    Website = table.Column<string>(type: "text", nullable: true),
                    LinkedInURL = table.Column<string>(type: "text", nullable: true),
                    SubmitterRole = table.Column<string>(type: "text", nullable: true),
                    TaxIdOrBusinessCode = table.Column<string>(type: "text", nullable: true),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    RequiresNewEvidence = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<int>(type: "integer", nullable: true),
                    ReviewedByUserUserID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestorKycSubmissions", x => x.SubmissionID);
                    table.ForeignKey(
                        name: "FK_InvestorKycSubmissions_Investors_InvestorID",
                        column: x => x.InvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InvestorKycSubmissions_Users_ReviewedByUserUserID",
                        column: x => x.ReviewedByUserUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateTable(
                name: "InvestorKycEvidenceFiles",
                columns: table => new
                {
                    EvidenceFileID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubmissionID = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: true),
                    Kind = table.Column<short>(type: "smallint", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvestorKycEvidenceFiles", x => x.EvidenceFileID);
                    table.ForeignKey(
                        name: "FK_InvestorKycEvidenceFiles_InvestorKycSubmissions_SubmissionID",
                        column: x => x.SubmissionID,
                        principalTable: "InvestorKycSubmissions",
                        principalColumn: "SubmissionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvestorKycEvidenceFiles_SubmissionID",
                table: "InvestorKycEvidenceFiles",
                column: "SubmissionID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorKycSubmissions_InvestorID",
                table: "InvestorKycSubmissions",
                column: "InvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_InvestorKycSubmissions_ReviewedByUserUserID",
                table: "InvestorKycSubmissions",
                column: "ReviewedByUserUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvestorKycEvidenceFiles");

            migrationBuilder.DropTable(
                name: "InvestorKycSubmissions");

            migrationBuilder.AddColumn<string>(
                name: "BusinessCode",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentOrganization",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentRoleTitle",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IDProofFileName",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IDProofFileURL",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvestmentProofFileName",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvestmentProofFileURL",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "InvestorType",
                table: "Investors",
                type: "smallint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionRemarks",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Remarks",
                table: "Investors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresNewEvidence",
                table: "Investors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SubmitterRole",
                table: "Investors",
                type: "text",
                nullable: true);
        }
    }
}
