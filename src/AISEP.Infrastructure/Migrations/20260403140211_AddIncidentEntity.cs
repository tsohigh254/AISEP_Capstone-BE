using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Removed DropColumn for ContactEmail, CurrentOrganization,
            // CurrentRoleTitle, ProofFileURL — these columns don't exist
            // in production DB (already removed or never created).

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    IncidentID = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Severity = table.Column<short>(type: "smallint", nullable: false),
                    Status = table.Column<short>(type: "smallint", nullable: false),
                    CreatedBy = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<int>(type: "integer", nullable: true),
                    RollbackNotes = table.Column<string>(type: "text", nullable: true),
                    IsRolledBack = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedByUserUserID = table.Column<int>(type: "integer", nullable: true),
                    ResolvedByUserUserID = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.IncidentID);
                    table.ForeignKey(
                        name: "FK_Incidents_Users_CreatedByUserUserID",
                        column: x => x.CreatedByUserUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                    table.ForeignKey(
                        name: "FK_Incidents_Users_ResolvedByUserUserID",
                        column: x => x.ResolvedByUserUserID,
                        principalTable: "Users",
                        principalColumn: "UserID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CreatedByUserUserID",
                table: "Incidents",
                column: "CreatedByUserUserID");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_ResolvedByUserUserID",
                table: "Incidents",
                column: "ResolvedByUserUserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Incidents");
        }
    }
}
