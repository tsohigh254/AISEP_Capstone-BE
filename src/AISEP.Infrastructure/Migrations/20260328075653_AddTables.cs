using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdvisorWeeklyScheduleTemplates",
                columns: table => new
                {
                    TemplateID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    DayOfWeek = table.Column<int>(type: "integer", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorWeeklyScheduleTemplates", x => x.TemplateID);
                    table.ForeignKey(
                        name: "FK_AdvisorWeeklyScheduleTemplates_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdvisorAvailableSlots",
                columns: table => new
                {
                    SlotID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    TemplateID = table.Column<int>(type: "integer", nullable: true),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsBooked = table.Column<bool>(type: "boolean", nullable: false),
                    BookedSessionID = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdvisorAvailableSlots", x => x.SlotID);
                    table.ForeignKey(
                        name: "FK_AdvisorAvailableSlots_AdvisorWeeklyScheduleTemplates_Templa~",
                        column: x => x.TemplateID,
                        principalTable: "AdvisorWeeklyScheduleTemplates",
                        principalColumn: "TemplateID",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AdvisorAvailableSlots_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AdvisorAvailableSlots_MentorshipSessions_BookedSessionID",
                        column: x => x.BookedSessionID,
                        principalTable: "MentorshipSessions",
                        principalColumn: "SessionID",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorAvailableSlots_AdvisorID",
                table: "AdvisorAvailableSlots",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorAvailableSlots_BookedSessionID",
                table: "AdvisorAvailableSlots",
                column: "BookedSessionID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorAvailableSlots_TemplateID",
                table: "AdvisorAvailableSlots",
                column: "TemplateID");

            migrationBuilder.CreateIndex(
                name: "IX_AdvisorWeeklyScheduleTemplates_AdvisorID",
                table: "AdvisorWeeklyScheduleTemplates",
                column: "AdvisorID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdvisorAvailableSlots");

            migrationBuilder.DropTable(
                name: "AdvisorWeeklyScheduleTemplates");
        }
    }
}
