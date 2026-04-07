using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartupKycSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StartupKycSubmissions",
                columns: table => new
                {
                    SubmissionID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    WorkflowStatus = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1),
                    ResultLabel = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0),
                    StartupVerificationType = table.Column<short>(type: "smallint", nullable: false),
                    LegalFullName = table.Column<string>(type: "text", nullable: true),
                    ProjectName = table.Column<string>(type: "text", nullable: true),
                    EnterpriseCode = table.Column<string>(type: "text", nullable: true),
                    RepresentativeFullName = table.Column<string>(type: "text", nullable: false),
                    RepresentativeRole = table.Column<string>(type: "text", nullable: false),
                    WorkEmail = table.Column<string>(type: "text", nullable: false),
                    PublicLink = table.Column<string>(type: "text", nullable: true),
                    Explanation = table.Column<string>(type: "text", nullable: true),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupKycSubmissions", x => x.SubmissionID);
                    table.ForeignKey(
                        name: "FK_StartupKycSubmissions_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StartupKycSubmissions_Users_ReviewedBy",
                        column: x => x.ReviewedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StartupKycEvidenceFiles",
                columns: table => new
                {
                    EvidenceFileID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubmissionID = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: true),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    StorageKey = table.Column<string>(type: "text", nullable: true),
                    Kind = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)3),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupKycEvidenceFiles", x => x.EvidenceFileID);
                    table.ForeignKey(
                        name: "FK_StartupKycEvidenceFiles_StartupKycSubmissions_SubmissionID",
                        column: x => x.SubmissionID,
                        principalTable: "StartupKycSubmissions",
                        principalColumn: "SubmissionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StartupKycRequestedItems",
                columns: table => new
                {
                    RequestedItemID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SubmissionID = table.Column<int>(type: "integer", nullable: false),
                    FieldKey = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupKycRequestedItems", x => x.RequestedItemID);
                    table.ForeignKey(
                        name: "FK_StartupKycRequestedItems_StartupKycSubmissions_SubmissionID",
                        column: x => x.SubmissionID,
                        principalTable: "StartupKycSubmissions",
                        principalColumn: "SubmissionID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StartupKycEvidenceFiles_SubmissionID",
                table: "StartupKycEvidenceFiles",
                column: "SubmissionID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupKycRequestedItems_SubmissionID",
                table: "StartupKycRequestedItems",
                column: "SubmissionID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupKycSubmissions_ReviewedBy",
                table: "StartupKycSubmissions",
                column: "ReviewedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StartupKycSubmissions_StartupID_IsActive",
                table: "StartupKycSubmissions",
                columns: new[] { "StartupID", "IsActive" },
                filter: "\"IsActive\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_StartupKycSubmissions_StartupID_Version",
                table: "StartupKycSubmissions",
                columns: new[] { "StartupID", "Version" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StartupKycEvidenceFiles");

            migrationBuilder.DropTable(
                name: "StartupKycRequestedItems");

            migrationBuilder.DropTable(
                name: "StartupKycSubmissions");
        }
    }
}
