using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMentorshipRequestedSlots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MentorshipRequestedSlots",
                columns: table => new
                {
                    SlotID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MentorshipID = table.Column<int>(type: "integer", nullable: false),
                    StartAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Timezone = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MentorshipRequestedSlots", x => x.SlotID);
                    table.ForeignKey(
                        name: "FK_MentorshipRequestedSlots_StartupAdvisorMentorships_Mentorsh~",
                        column: x => x.MentorshipID,
                        principalTable: "StartupAdvisorMentorships",
                        principalColumn: "MentorshipID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MentorshipRequestedSlots_MentorshipID",
                table: "MentorshipRequestedSlots",
                column: "MentorshipID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MentorshipRequestedSlots");
        }
    }
}
