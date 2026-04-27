using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundedAtToMentorship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "BusinessPlanOverallScore",
                table: "StartupPotentialScores",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "PitchDeckOverallScore",
                table: "StartupPotentialScores",
                type: "real",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "StartupAdvisorMentorships",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BusinessPlanOverallScore",
                table: "StartupPotentialScores");

            migrationBuilder.DropColumn(
                name: "PitchDeckOverallScore",
                table: "StartupPotentialScores");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "StartupAdvisorMentorships");
        }
    }
}
