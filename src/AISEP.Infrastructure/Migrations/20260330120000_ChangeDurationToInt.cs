using AISEP.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260330120000_ChangeDurationToInt")]
    /// <inheritdoc />
    public partial class ChangeDurationToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safe cast: numeric strings ("60") convert to int; non-numeric values ("3 months") become NULL.
            migrationBuilder.Sql(@"
                ALTER TABLE ""StartupAdvisorMentorships""
                ALTER COLUMN ""ExpectedDuration"" TYPE integer
                USING CASE
                    WHEN ""ExpectedDuration"" ~ '^[0-9]+$' THEN ""ExpectedDuration""::integer
                    ELSE NULL
                END;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""StartupAdvisorMentorships""
                ALTER COLUMN ""ExpectedDuration"" TYPE text
                USING ""ExpectedDuration""::text;
            ");
        }
    }
}
