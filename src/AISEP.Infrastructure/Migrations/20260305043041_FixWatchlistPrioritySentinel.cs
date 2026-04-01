using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AISEP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixWatchlistPrioritySentinel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "Priority",
                table: "InvestorWatchlists",
                type: "smallint",
                nullable: true,
                defaultValueSql: "1",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Fill NULLs before restoring NOT NULL constraint
            migrationBuilder.Sql("""UPDATE "InvestorWatchlists" SET "Priority" = 1 WHERE "Priority" IS NULL;""");

            migrationBuilder.AlterColumn<short>(
                name: "Priority",
                table: "InvestorWatchlists",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1,
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true,
                oldDefaultValueSql: "1");
        }
    }
}
