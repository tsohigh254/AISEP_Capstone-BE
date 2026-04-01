using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAdvisorAndItsRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvisorAchievements");

            migrationBuilder.DropTable(
                name: "AdvisorExpertises");

            migrationBuilder.DropTable(
                name: "ProfileViews");

            migrationBuilder.DropColumn(
                name: "Company",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "ProfileCompleteness",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "Website",
                table: "Advisors");

            migrationBuilder.DropColumn(
                name: "Industry",
                table: "AdvisorIndustryFocuses");

            migrationBuilder.AddColumn<int>(
                name: "IndustryID",
                table: "AdvisorIndustryFocuses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorIndustryFocuses_IndustryID",
                table: "AdvisorIndustryFocuses",
                column: "IndustryID");

            migrationBuilder.AddForeignKey(
                name: "FK_AdvisorIndustryFocuses_Industries_IndustryID",
                table: "AdvisorIndustryFocuses",
                column: "IndustryID",
                principalTable: "Industries",
                principalColumn: "IndustryID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdvisorIndustryFocuses_Industries_IndustryID",
                table: "AdvisorIndustryFocuses");

            migrationBuilder.DropIndex(
                name: "IX_AdvisorIndustryFocuses_IndustryID",
                table: "AdvisorIndustryFocuses");

            migrationBuilder.DropColumn(
                name: "IndustryID",
                table: "AdvisorIndustryFocuses");

            migrationBuilder.AddColumn<string>(
                name: "Company",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileCompleteness",
                table: "Advisors",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Website",
                table: "Advisors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Industry",
                table: "AdvisorIndustryFocuses",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AdvisorAchievements",
                columns: table => new
                {
                    AchievementID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    AchievementType = table.Column<short>(type: "smallint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "text", nullable: false),
                    URL = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorAchievements", x => x.AchievementID);
                    table.ForeignKey(
                        name: "FK_AdvisorAchievements_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdvisorExpertises",
                columns: table => new
                {
                    ExpertiseID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    ProficiencyLevel = table.Column<short>(type: "smallint", nullable: true),
                    SubTopic = table.Column<string>(type: "text", nullable: true),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorExpertises", x => x.ExpertiseID);
                    table.ForeignKey(
                        name: "FK_AdvisorExpertises_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileViews",
                columns: table => new
                {
                    ViewID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ViewedAdvisorID = table.Column<int>(type: "integer", nullable: true),
                    ViewedInvestorID = table.Column<int>(type: "integer", nullable: true),
                    ViewedStartupID = table.Column<int>(type: "integer", nullable: true),
                    ViewerUserID = table.Column<int>(type: "integer", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileViews", x => x.ViewID);
                    table.ForeignKey(
                        name: "FK_ProfileViews_Advisors_ViewedAdvisorID",
                        column: x => x.ViewedAdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileViews_Investors_ViewedInvestorID",
                        column: x => x.ViewedInvestorID,
                        principalTable: "Investors",
                        principalColumn: "InvestorID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileViews_Startups_ViewedStartupID",
                        column: x => x.ViewedStartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProfileViews_Users_ViewerUserID",
                        column: x => x.ViewerUserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorAchievements_AdvisorID",
                table: "AdvisorAchievements",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorExpertises_AdvisorID",
                table: "AdvisorExpertises",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileViews_ViewedAdvisorID",
                table: "ProfileViews",
                column: "ViewedAdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileViews_ViewedInvestorID",
                table: "ProfileViews",
                column: "ViewedInvestorID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileViews_ViewedStartupID",
                table: "ProfileViews",
                column: "ViewedStartupID");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileViews_ViewerUserID",
                table: "ProfileViews",
                column: "ViewerUserID");
        }
    }
}
