using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSessionTabel : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MentorshipFeedbacks_SessionID",
                table: "MentorshipFeedbacks");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "ExpectedScope",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "ObligationSummary",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "PreferredFormat",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "SpecificQuestions",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "ActionItems",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "AdvisorConfirmedConductedAt",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "AdvisorInternalNotes",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "ConductedConfirmedAt",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "KeyInsights",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "NextSteps",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "RecommendedResources",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "SessionFormat",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "StartupConfirmedConductedAt",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "StartupNotes",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "MentorshipSessions");

            migrationBuilder.DropColumn(
                name: "CalendarConnected",
                table: "AdvisorAvailabilities");

            // Add temporary column to hold converted values
            migrationBuilder.AddColumn<short>(
                name: "SessionStatus_Temp",
                table: "MentorshipSessions",
                type: "smallint",
                nullable: true);

            // Convert existing text values to smallint
            migrationBuilder.Sql(@"
                UPDATE ""MentorshipSessions""
                SET ""SessionStatus_Temp"" = CASE
                    WHEN ""SessionStatus"" = 'Pending' THEN 0
                    WHEN ""SessionStatus"" = 'Confirmed' THEN 1
                    WHEN ""SessionStatus"" = 'Cancelled' THEN 2
                    WHEN ""SessionStatus"" = 'Completed' THEN 3
                    ELSE 0
                END
                WHERE ""SessionStatus"" IS NOT NULL;
            ");

            // Drop the old text column
            migrationBuilder.DropColumn(
                name: "SessionStatus",
                table: "MentorshipSessions");

            // Rename temp column to original name
            migrationBuilder.RenameColumn(
                name: "SessionStatus_Temp",
                table: "MentorshipSessions",
                newName: "SessionStatus");

            // Alter to make it non-nullable with default value
            migrationBuilder.AlterColumn<short>(
                name: "SessionStatus",
                table: "MentorshipSessions",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ScheduledStartAt",
                table: "MentorshipSessions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MeetingURL",
                table: "MentorshipSessions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipFeedbacks_SessionID",
                table: "MentorshipFeedbacks",
                column: "SessionID",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MentorshipFeedbacks_SessionID",
                table: "MentorshipFeedbacks");

            // Add temporary column to hold converted values
            migrationBuilder.AddColumn<string>(
                name: "SessionStatus_Temp",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            // Convert smallint back to text
            migrationBuilder.Sql(@"
                UPDATE ""MentorshipSessions""
                SET ""SessionStatus_Temp"" = CASE
                    WHEN ""SessionStatus"" = 0 THEN 'Pending'
                    WHEN ""SessionStatus"" = 1 THEN 'Confirmed'
                    WHEN ""SessionStatus"" = 2 THEN 'Cancelled'
                    WHEN ""SessionStatus"" = 3 THEN 'Completed'
                    ELSE 'Pending'
                END;
            ");

            // Drop the smallint column
            migrationBuilder.DropColumn(
                name: "SessionStatus",
                table: "MentorshipSessions");

            // Rename temp column back to original name
            migrationBuilder.RenameColumn(
                name: "SessionStatus_Temp",
                table: "MentorshipSessions",
                newName: "SessionStatus");

            // Alter back to text nullable
            migrationBuilder.AlterColumn<string>(
                name: "SessionStatus",
                table: "MentorshipSessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ScheduledStartAt",
                table: "MentorshipSessions",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "MeetingURL",
                table: "MentorshipSessions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "StartupAdvisorMentorships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpectedScope",
                table: "StartupAdvisorMentorships",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObligationSummary",
                table: "StartupAdvisorMentorships",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredFormat",
                table: "StartupAdvisorMentorships",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecificQuestions",
                table: "StartupAdvisorMentorships",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActionItems",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AdvisorConfirmedConductedAt",
                table: "MentorshipSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvisorInternalNotes",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConductedConfirmedAt",
                table: "MentorshipSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KeyInsights",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NextSteps",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedResources",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SessionFormat",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartupConfirmedConductedAt",
                table: "MentorshipSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartupNotes",
                table: "MentorshipSessions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "MentorshipSessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CalendarConnected",
                table: "AdvisorAvailabilities",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipFeedbacks_SessionID",
                table: "MentorshipFeedbacks",
                column: "SessionID");
        }
    }
}
