using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIssueReports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IssueReports",
                columns: table => new
                {
                    IssueReportID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReporterUserID = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<short>(type: "smallint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    RelatedEntityType = table.Column<string>(type: "text", nullable: true),
                    RelatedEntityID = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    StaffNote = table.Column<string>(type: "text", nullable: true),
                    AssignedToStaffID = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueReports", x => x.IssueReportID);
                    table.ForeignKey(
                        name: "FK_IssueReports_Users_AssignedToStaffID",
                        column: x => x.AssignedToStaffID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueReports_Users_ReporterUserID",
                        column: x => x.ReporterUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueReportAttachments",
                columns: table => new
                {
                    AttachmentID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IssueReportID = table.Column<int>(type: "integer", nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    FileSize = table.Column<long>(type: "bigint", nullable: true),
                    MimeType = table.Column<string>(type: "text", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueReportAttachments", x => x.AttachmentID);
                    table.ForeignKey(
                        name: "FK_IssueReportAttachments_IssueReports_IssueReportID",
                        column: x => x.IssueReportID,
                        principalTable: "IssueReports",
                        principalColumn: "IssueReportID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueReportAttachments_IssueReportID",
                table: "IssueReportAttachments",
                column: "IssueReportID");

            migrationBuilder.CreateIndex(
                name: "IX_IssueReports_AssignedToStaffID",
                table: "IssueReports",
                column: "AssignedToStaffID");

            migrationBuilder.CreateIndex(
                name: "IX_IssueReports_ReporterUserID",
                table: "IssueReports",
                column: "ReporterUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueReportAttachments");

            migrationBuilder.DropTable(
                name: "IssueReports");
        }
    }
}
