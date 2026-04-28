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
            // BusinessPlanOverallScore / PitchDeckOverallScore đã được migration
            // 20260427005635_AddPitchDeckBusinessPlanOverallScoreColumns thêm (idempotent IF NOT EXISTS).
            // Không AddColumn lại — tránh 42701 column already exists trên DB đã có cột.

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
                name: "RefundedAt",
                table: "StartupAdvisorMentorships");
        }
    }
}
