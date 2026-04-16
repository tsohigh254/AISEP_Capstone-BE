using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultingOversight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPayoutEligible",
                table: "StartupAdvisorMentorships",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DisputeReason",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MarkedAt",
                table: "MentorshipSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MarkedByStaffID",
                table: "MentorshipSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNote",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "ReportReviewStatus",
                table: "MentorshipReports",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "MentorshipReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByStaffID",
                table: "MentorshipReports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StaffReviewNote",
                table: "MentorshipReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupersededByReportID",
                table: "MentorshipReports",
                type: "integer",
                nullable: true);

            // Data migration: reports previously marked ReviewedByStaff=true → Passed(1)
            migrationBuilder.Sql(
                """
                UPDATE "MentorshipReports"
                SET "ReportReviewStatus" = 1
                WHERE "ReviewedByStaff" = true;
                """);

            migrationBuilder.DropColumn(
                name: "ReviewedByStaff",
                table: "MentorshipReports");

            // Data migration: cancel stale ProposedByStartup/ProposedByAdvisor sessions
            // where the same mentorship already has a Scheduled/InProgress/Conducted/Completed session
            migrationBuilder.Sql(
                """
                UPDATE "MentorshipSessions"
                SET "SessionStatus" = 'Cancelled',
                    "UpdatedAt" = NOW() AT TIME ZONE 'UTC'
                WHERE "SessionStatus" IN ('ProposedByStartup', 'ProposedByAdvisor')
                  AND "MentorshipID" IN (
                      SELECT DISTINCT "MentorshipID"
                      FROM "MentorshipSessions"
                      WHERE "SessionStatus" IN ('Scheduled', 'InProgress', 'Conducted', 'Completed')
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPayoutEligible",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "DisputeReason",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "MarkedAt",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "MarkedByStaffID",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "ResolutionNote",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "ReportReviewStatus",
                table: "MentorshipReports");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "MentorshipReports");

            migrationBuilder.DropColumn(
                name: "ReviewedByStaffID",
                table: "MentorshipReports");

            migrationBuilder.DropColumn(
                name: "StaffReviewNote",
                table: "MentorshipReports");

            migrationBuilder.DropColumn(
                name: "SupersededByReportID",
                table: "MentorshipReports");

            migrationBuilder.AddColumn<bool>(
                name: "ReviewedByStaff",
                table: "MentorshipReports",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
