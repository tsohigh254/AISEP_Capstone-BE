using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeedbackAdvisorResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AdvisorRespondedAt",
                table: "MentorshipFeedbacks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdvisorResponseText",
                table: "MentorshipFeedbacks",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvisorRespondedAt",
                table: "MentorshipFeedbacks");

            migrationBuilder.DropColumn(
                name: "AdvisorResponseText",
                table: "MentorshipFeedbacks");
        }
    }
}
