using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStartupAdvisorBookmarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StartupAdvisorBookmarks",
                columns: table => new
                {
                    BookmarkID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StartupID = table.Column<int>(type: "integer", nullable: false),
                    AdvisorID = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StartupAdvisorBookmarks", x => x.BookmarkID);
                    table.ForeignKey(
                        name: "FK_StartupAdvisorBookmarks_Advisors_AdvisorID",
                        column: x => x.AdvisorID,
                        principalTable: "Advisors",
                        principalColumn: "AdvisorID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StartupAdvisorBookmarks_Startups_StartupID",
                        column: x => x.StartupID,
                        principalTable: "Startups",
                        principalColumn: "StartupID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StartupAdvisorBookmarks_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StartupAdvisorBookmarks_AdvisorID",
                table: "StartupAdvisorBookmarks",
                column: "AdvisorID");

            migrationBuilder.CreateIndex(
                name: "IX_StartupAdvisorBookmarks_CreatedBy",
                table: "StartupAdvisorBookmarks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_StartupAdvisorBookmarks_Unique",
                table: "StartupAdvisorBookmarks",
                columns: new[] { "StartupID", "AdvisorID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StartupAdvisorBookmarks");
        }
    }
}
