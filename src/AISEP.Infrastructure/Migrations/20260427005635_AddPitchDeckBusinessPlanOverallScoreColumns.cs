using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPitchDeckBusinessPlanOverallScoreColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: prod DB had these columns added manually as a hotfix
            // before this migration existed. IF NOT EXISTS lets the same migration
            // succeed on prod (no-op) and on fresh environments (adds columns).
            migrationBuilder.Sql(@"
                ALTER TABLE ""StartupPotentialScores""
                    ADD COLUMN IF NOT EXISTS ""BusinessPlanOverallScore"" real NULL,
                    ADD COLUMN IF NOT EXISTS ""PitchDeckOverallScore"" real NULL;
            ");
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
        }
    }
}
