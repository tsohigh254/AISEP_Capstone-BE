using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorshipCancelFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InProgressAt",
                table: "StartupAdvisorMentorships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                table: "StartupAdvisorMentorships",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledBy",
                table: "StartupAdvisorMentorships",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "StartupAdvisorMentorships",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InProgressAt",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "CancelledBy",
                table: "StartupAdvisorMentorships");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "StartupAdvisorMentorships");
        }
    }
}
